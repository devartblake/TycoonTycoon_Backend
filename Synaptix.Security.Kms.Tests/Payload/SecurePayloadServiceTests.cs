using System.Security.Cryptography;
using System.Text;
using Synaptix.Security.Kms.Application.Payload;
using Synaptix.Security.Kms.Contracts.Models;
using Synaptix.Security.Kms.Tests.Helpers;

namespace Synaptix.Security.Kms.Tests.Payload;

public sealed class SecurePayloadServiceTests
{
    private static InMemorySessionStore BuildStore(out Guid sessionId, bool expired = false)
    {
        sessionId = Guid.NewGuid();
        var c2sKey = RandomNumberGenerator.GetBytes(32);
        var s2cKey = RandomNumberGenerator.GetBytes(32);
        var expiry = expired
            ? DateTimeOffset.UtcNow.AddMinutes(-1)
            : DateTimeOffset.UtcNow.AddMinutes(30);

        var session = new SecureSession(
            sessionId, "user-1", "device-1",
            "syn-sec-v1", "X25519-HKDF-SHA256-AES256GCM",
            c2sKey, s2cKey,
            DateTimeOffset.UtcNow, expiry, 0L);

        var store = new InMemorySessionStore();
        store.SaveAsync(session, default).GetAwaiter().GetResult();
        return store;
    }

    private static SecurePayloadService BuildService(InMemorySessionStore store)
        => new(store, new InMemoryReplayProtectionStore());

    private static EncryptedPayload EncryptClientPayload(
        byte[] key,
        byte[] plaintext,
        string contentType,
        DateTimeOffset encryptedAtUtc,
        string? aad)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(
            nonce,
            plaintext,
            ciphertext,
            tag,
            string.IsNullOrEmpty(aad) ? null : Encoding.UTF8.GetBytes(aad));

        return new EncryptedPayload(
            Base64UrlEncode(ciphertext),
            Base64UrlEncode(nonce),
            Base64UrlEncode(tag),
            contentType,
            encryptedAtUtc);
    }

    [Fact]
    public async Task EncryptThenDecrypt_RoundtripRecovery()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);

        var plaintext = Encoding.UTF8.GetBytes(@"{""action"":""purchase"",""sku"":""gold-500""}");
        var encrypted = await svc.EncryptAsync(sessionId, plaintext, "application/json", default, "response-aad");

        var session = store[sessionId]!;
        var roundtripSessionId = Guid.NewGuid();
        var roundtripSession = session with
        {
            SessionId = roundtripSessionId,
            ClientToServerKey = session.ServerToClientKey,
            ServerToClientKey = session.ClientToServerKey
        };
        await store.SaveAsync(roundtripSession, default);

        var (decrypted, contentType) = await svc.DecryptAsync(
            roundtripSessionId,
            encrypted,
            default,
            1,
            "nonce-1",
            "response-aad",
            "user-1");

        decrypted.Should().BeEquivalentTo(plaintext);
        contentType.Should().Be("application/json");
    }

    [Fact]
    public async Task DecryptAsync_TamperedCiphertext_Throws()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);

        var plaintext = Encoding.UTF8.GetBytes("sensitive data");
        var encrypted = await svc.EncryptAsync(sessionId, plaintext, "application/json", default);

        var tampered = encrypted with { Ciphertext = encrypted.Ciphertext[..^4] + "AAAA" };

        var act = () => svc.DecryptAsync(sessionId, tampered, default, 1, "nonce-2", null, "user-1");
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task EncryptAsync_ExpiredSession_Throws()
    {
        var store = BuildStore(out var sessionId, expired: true);
        var svc = BuildService(store);

        var act = () => svc.EncryptAsync(sessionId, [1, 2, 3], "application/json", default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task EncryptAsync_UnknownSession_Throws()
    {
        var store = new InMemorySessionStore();
        var svc = BuildService(store);

        var act = () => svc.EncryptAsync(Guid.NewGuid(), [1, 2, 3], "application/json", default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task EncryptAsync_DifferentNonceEachCall_ProducesDifferentCiphertext()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);
        var plaintext = Encoding.UTF8.GetBytes("same plaintext");

        var first = await svc.EncryptAsync(sessionId, plaintext, "application/json", default);
        var second = await svc.EncryptAsync(sessionId, plaintext, "application/json", default);

        first.Nonce.Should().NotBe(second.Nonce);
        first.Ciphertext.Should().NotBe(second.Ciphertext);
    }

    [Fact]
    public async Task DecryptAsync_ClientPayloadWithMatchingAad_ReturnsPlaintext()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);
        var session = store[sessionId]!;
        var aad = "syn-sec-v1|request|POST|/store/purchase|" + sessionId.ToString("N");
        var plaintext = Encoding.UTF8.GetBytes(@"{""sku"":""starter-pack""}");
        var payload = EncryptClientPayload(session.ClientToServerKey, plaintext, "application/json", DateTimeOffset.UtcNow, aad);

        var (decrypted, contentType) = await svc.DecryptAsync(sessionId, payload, default, 11, "nonce-aad-ok", aad, "user-1");

        decrypted.Should().BeEquivalentTo(plaintext);
        contentType.Should().Be("application/json");
    }

    [Fact]
    public async Task DecryptAsync_AadMismatch_ThrowsAuthenticationFailure()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);
        var session = store[sessionId]!;
        var payload = EncryptClientPayload(
            session.ClientToServerKey,
            Encoding.UTF8.GetBytes("payload"),
            "application/json",
            DateTimeOffset.UtcNow,
            "expected-aad");

        var act = () => svc.DecryptAsync(sessionId, payload, default, 12, "nonce-aad-bad", "wrong-aad", "user-1");

        await act.Should().ThrowAsync<AuthenticationTagMismatchException>();
    }

    [Fact]
    public async Task DecryptAsync_ReplayedSequence_ThrowsReplayDetected()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);
        var session = store[sessionId]!;
        var aad = "aad-replay-sequence";
        var first = EncryptClientPayload(session.ClientToServerKey, Encoding.UTF8.GetBytes("one"), "text/plain", DateTimeOffset.UtcNow, aad);
        var second = EncryptClientPayload(session.ClientToServerKey, Encoding.UTF8.GetBytes("two"), "text/plain", DateTimeOffset.UtcNow, aad);

        await svc.DecryptAsync(sessionId, first, default, 21, "nonce-replay-1", aad, "user-1");
        var act = () => svc.DecryptAsync(sessionId, second, default, 21, "nonce-replay-2", aad, "user-1");

        await act.Should().ThrowAsync<SecurePayloadException>()
            .Where(ex => ex.Code == "replay_detected");
    }

    [Fact]
    public async Task DecryptAsync_ReplayedReplayNonce_ThrowsReplayDetected()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);
        var session = store[sessionId]!;
        var aad = "aad-replay-nonce";
        var first = EncryptClientPayload(session.ClientToServerKey, Encoding.UTF8.GetBytes("one"), "text/plain", DateTimeOffset.UtcNow, aad);
        var second = EncryptClientPayload(session.ClientToServerKey, Encoding.UTF8.GetBytes("two"), "text/plain", DateTimeOffset.UtcNow, aad);

        await svc.DecryptAsync(sessionId, first, default, 31, "nonce-replay-shared", aad, "user-1");
        var act = () => svc.DecryptAsync(sessionId, second, default, 32, "nonce-replay-shared", aad, "user-1");

        await act.Should().ThrowAsync<SecurePayloadException>()
            .Where(ex => ex.Code == "replay_detected");
    }

    [Fact]
    public async Task DecryptAsync_ExpiredTimestamp_ThrowsPayloadExpired()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);
        var session = store[sessionId]!;
        var payload = EncryptClientPayload(
            session.ClientToServerKey,
            Encoding.UTF8.GetBytes("old"),
            "text/plain",
            DateTimeOffset.UtcNow.AddMinutes(-10),
            "aad-expired");

        var act = () => svc.DecryptAsync(sessionId, payload, default, 41, "nonce-expired", "aad-expired", "user-1");

        await act.Should().ThrowAsync<SecurePayloadException>()
            .Where(ex => ex.Code == "payload_expired");
    }

    [Fact]
    public async Task DecryptAsync_SubjectMismatch_ThrowsSubjectMismatch()
    {
        var store = BuildStore(out var sessionId);
        var svc = BuildService(store);
        var session = store[sessionId]!;
        var payload = EncryptClientPayload(
            session.ClientToServerKey,
            Encoding.UTF8.GetBytes("subject"),
            "text/plain",
            DateTimeOffset.UtcNow,
            "aad-subject");

        var act = () => svc.DecryptAsync(sessionId, payload, default, 51, "nonce-subject", "aad-subject", "user-2");

        await act.Should().ThrowAsync<SecurePayloadException>()
            .Where(ex => ex.Code == "session_subject_mismatch");
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
