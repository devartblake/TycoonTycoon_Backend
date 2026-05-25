using System.Security.Cryptography;
using System.Text;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Application.Payload;

public sealed class SecurePayloadService(
    ISessionStore store,
    IReplayProtectionStore replayProtection) : ISecurePayloadProtector
{
    private const int NonceSizeBytes = 12;  // 96-bit nonce for AES-GCM
    private const int TagSizeBytes = 16;    // 128-bit authentication tag
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    public async Task<EncryptedPayload> EncryptAsync(
        Guid sessionId,
        byte[] plaintext,
        string contentType,
        CancellationToken ct,
        string? aad = null,
        string direction = "server-to-client")
    {
        var session = await RequireSessionAsync(sessionId, ct);

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(KeyForDirection(session, direction), TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, AdditionalData(aad));

        return new EncryptedPayload(
            Base64UrlEncode(ciphertext),
            Base64UrlEncode(nonce),
            Base64UrlEncode(tag),
            contentType,
            DateTimeOffset.UtcNow);
    }

    public async Task<(byte[] Plaintext, string ContentType)> DecryptAsync(
        Guid sessionId,
        EncryptedPayload payload,
        CancellationToken ct,
        long? sequenceNumber = null,
        string? replayNonce = null,
        string? aad = null,
        string? subjectId = null,
        string direction = "client-to-server",
        bool enforceReplay = true)
    {
        var session = await RequireSessionAsync(sessionId, ct);
        ValidateSubject(session, subjectId);
        if (enforceReplay)
            ValidateReplayMetadata(sequenceNumber, replayNonce);
        ValidateTimestamp(payload.EncryptedAtUtc);

        var ciphertext = Base64UrlDecode(payload.Ciphertext);
        var nonce = Base64UrlDecode(payload.Nonce);
        var tag = Base64UrlDecode(payload.Mac);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(KeyForDirection(session, direction), TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, AdditionalData(aad));

        if (enforceReplay)
        {
            var ttl = ReplayTtl(session);
            var accepted = await replayProtection.TryAcceptAsync(
                sessionId,
                sequenceNumber!.Value,
                replayNonce!,
                ttl,
                ct);
            if (!accepted)
                throw new SecurePayloadException("replay_detected", "Secure payload sequence or replay nonce has already been used.");
        }

        return (plaintext, payload.ContentType);
    }

    private static byte[] KeyForDirection(SecureSession session, string? direction)
    {
        return string.Equals(direction, "client-to-server", StringComparison.OrdinalIgnoreCase)
            ? session.ClientToServerKey
            : string.Equals(direction, "server-to-client", StringComparison.OrdinalIgnoreCase)
                ? session.ServerToClientKey
                : throw new SecurePayloadException("direction_invalid", "Secure payload direction must be client-to-server or server-to-client.");
    }

    private async Task<SecureSession> RequireSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await store.GetAsync(sessionId, ct)
            ?? throw new InvalidOperationException($"Session {sessionId} not found or expired.");

        if (session.ExpiresAtUtc < DateTimeOffset.UtcNow)
            throw new InvalidOperationException($"Session {sessionId} has expired.");

        return session;
    }

    private static void ValidateSubject(SecureSession session, string? subjectId)
    {
        if (!string.IsNullOrWhiteSpace(subjectId)
            && !string.Equals(session.SubjectId, subjectId, StringComparison.Ordinal))
        {
            throw new SecurePayloadException("session_subject_mismatch", "Secure session subject does not match the authenticated subject.");
        }
    }

    private static void ValidateReplayMetadata(long? sequenceNumber, string? replayNonce)
    {
        if (sequenceNumber is null or <= 0)
            throw new SecurePayloadException("sequence_invalid", "A positive secure payload sequence number is required.");

        if (string.IsNullOrWhiteSpace(replayNonce))
            throw new SecurePayloadException("replay_nonce_required", "A secure payload replay nonce is required.");

        if (replayNonce.Length > 256)
            throw new SecurePayloadException("replay_nonce_invalid", "Secure payload replay nonce is too long.");
    }

    private static void ValidateTimestamp(DateTimeOffset encryptedAtUtc)
    {
        var now = DateTimeOffset.UtcNow;
        if (encryptedAtUtc < now.Subtract(MaxClockSkew) || encryptedAtUtc > now.Add(MaxClockSkew))
        {
            throw new SecurePayloadException("payload_expired", "Secure payload timestamp is outside the allowed clock skew window.");
        }
    }

    private static TimeSpan ReplayTtl(SecureSession session)
    {
        var remainingSessionTtl = session.ExpiresAtUtc - DateTimeOffset.UtcNow;
        if (remainingSessionTtl <= TimeSpan.Zero)
            return TimeSpan.FromSeconds(1);

        return remainingSessionTtl < MaxClockSkew ? remainingSessionTtl : MaxClockSkew;
    }

    private static byte[]? AdditionalData(string? aad)
        => string.IsNullOrEmpty(aad) ? null : Encoding.UTF8.GetBytes(aad);

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded
        };
        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
