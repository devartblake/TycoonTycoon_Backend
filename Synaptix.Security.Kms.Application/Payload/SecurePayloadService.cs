using System.Security.Cryptography;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Application.Payload;

public sealed class SecurePayloadService(ISessionStore store) : ISecurePayloadProtector
{
    private const int NonceSizeBytes = 12;  // 96-bit nonce for AES-GCM
    private const int TagSizeBytes = 16;    // 128-bit authentication tag

    public async Task<EncryptedPayload> EncryptAsync(
        Guid sessionId, byte[] plaintext, string contentType, CancellationToken ct)
    {
        var session = await RequireSessionAsync(sessionId, ct);

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(session.ServerToClientKey, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return new EncryptedPayload(
            Base64UrlEncode(ciphertext),
            Base64UrlEncode(nonce),
            Base64UrlEncode(tag),
            contentType,
            DateTimeOffset.UtcNow);
    }

    public async Task<(byte[] Plaintext, string ContentType)> DecryptAsync(
        Guid sessionId, EncryptedPayload payload, CancellationToken ct)
    {
        var session = await RequireSessionAsync(sessionId, ct);

        var ciphertext = Base64UrlDecode(payload.Ciphertext);
        var nonce = Base64UrlDecode(payload.Nonce);
        var tag = Base64UrlDecode(payload.Mac);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(session.ClientToServerKey, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return (plaintext, payload.ContentType);
    }

    private async Task<SecureSession> RequireSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await store.GetAsync(sessionId, ct)
            ?? throw new InvalidOperationException($"Session {sessionId} not found or expired.");

        if (session.ExpiresAtUtc < DateTimeOffset.UtcNow)
            throw new InvalidOperationException($"Session {sessionId} has expired.");

        return session;
    }

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
