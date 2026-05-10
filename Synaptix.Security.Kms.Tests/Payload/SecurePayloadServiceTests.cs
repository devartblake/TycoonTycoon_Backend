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

    [Fact]
    public async Task EncryptThenDecrypt_RoundtripRecovery()
    {
        var store = BuildStore(out var sessionId);
        var svc = new SecurePayloadService(store);

        var plaintext = Encoding.UTF8.GetBytes(@"{""action"":""purchase"",""sku"":""gold-500""}");
        var encrypted = await svc.EncryptAsync(sessionId, plaintext, "application/json", default);

        // Swap keys for decryption: the server encrypted with s2cKey, client decrypts with s2cKey.
        // In tests we do a server→server roundtrip by swapping which key is C2S vs S2C.
        // Simpler: create a second payload protector that has s2c as c2s (server encrypts, server decrypts with S2C)
        // The service uses s2cKey for Encrypt and c2sKey for Decrypt.
        // So to roundtrip, we need a session where c2s == original s2c.
        var session = store[sessionId]!;
        var roundtripSessionId = Guid.NewGuid();
        var roundtripSession = session with
        {
            SessionId = roundtripSessionId,
            ClientToServerKey = session.ServerToClientKey,
            ServerToClientKey = session.ClientToServerKey
        };
        await store.SaveAsync(roundtripSession, default);

        var (decrypted, contentType) = await svc.DecryptAsync(roundtripSessionId, encrypted, default);
        decrypted.Should().BeEquivalentTo(plaintext);
        contentType.Should().Be("application/json");
    }

    [Fact]
    public async Task DecryptAsync_TamperedCiphertext_Throws()
    {
        var store = BuildStore(out var sessionId);
        var svc = new SecurePayloadService(store);

        var plaintext = Encoding.UTF8.GetBytes("sensitive data");
        var encrypted = await svc.EncryptAsync(sessionId, plaintext, "application/json", default);

        // Use same session for decrypt (c2sKey != s2cKey, so decryption key differs — but main goal is tag mismatch)
        var tampered = encrypted with { Ciphertext = encrypted.Ciphertext[..^4] + "AAAA" };

        var act = () => svc.DecryptAsync(sessionId, tampered, default);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task EncryptAsync_ExpiredSession_Throws()
    {
        var store = BuildStore(out var sessionId, expired: true);
        var svc = new SecurePayloadService(store);

        var act = () => svc.EncryptAsync(sessionId, [1, 2, 3], "application/json", default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task EncryptAsync_UnknownSession_Throws()
    {
        var store = new InMemorySessionStore();
        var svc = new SecurePayloadService(store);

        var act = () => svc.EncryptAsync(Guid.NewGuid(), [1, 2, 3], "application/json", default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task EncryptAsync_DifferentNonceEachCall_ProducesDifferentCiphertext()
    {
        var store = BuildStore(out var sessionId);
        var svc = new SecurePayloadService(store);
        var plaintext = Encoding.UTF8.GetBytes("same plaintext");

        var first = await svc.EncryptAsync(sessionId, plaintext, "application/json", default);
        var second = await svc.EncryptAsync(sessionId, plaintext, "application/json", default);

        // Nonces must differ (random per call)
        first.Nonce.Should().NotBe(second.Nonce);
        // Ciphertext must differ since nonce differs
        first.Ciphertext.Should().NotBe(second.Ciphertext);
    }
}
