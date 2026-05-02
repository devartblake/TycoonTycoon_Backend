using System.Security.Cryptography;
using System.Text;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;
using Synaptix.Security.Kms.Contracts.Suites;

namespace Synaptix.Security.Kms.Application.Sessions;

public sealed class SecureSessionService(ISessionStore store) : ISecureSessionService
{
    private const string ProtocolVersion = "syn-sec-v1";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(30);

    // X25519 OID — supported on .NET 8+ Linux/macOS/Windows via CNG
    private static readonly ECCurve Curve25519 = ECCurve.CreateFromOid(new Oid("1.3.101.110"));

    public async Task<StartSessionResult> StartAsync(
        string subjectId, StartSessionCommand command, CancellationToken ct)
    {
        var suite = SelectSuite(command.SupportedSuites);
        var sessionId = Guid.NewGuid();
        var serverNonceBytes = RandomNumberGenerator.GetBytes(24);

        // Generate server ephemeral X25519 key pair
        using var serverEcdh = ECDiffieHellman.Create(Curve25519);
        var serverPublicKeySpki = serverEcdh.ExportSubjectPublicKeyInfo();

        // Import client public key and derive shared secret
        var clientPubKeyBytes = Base64UrlDecode(command.ClientPublicKey);
        using var clientEcdh = ECDiffieHellman.Create();
        clientEcdh.ImportSubjectPublicKeyInfo(clientPubKeyBytes, out _);

        var sharedSecret = serverEcdh.DeriveRawSecretAgreement(clientEcdh.PublicKey);

        // HKDF-SHA256 key derivation matching the protocol spec
        var clientNonceBytes = Base64UrlDecode(command.ClientNonce);
        var salt = SHA256.HashData(
        [
            ..clientNonceBytes,
            ..serverNonceBytes,
            ..sessionId.ToByteArray()
        ]);

        var c2sKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256, sharedSecret, 32, salt,
            "synaptix:c2s:v1"u8.ToArray());

        var s2cKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256, sharedSecret, 32, salt,
            "synaptix:s2c:v1"u8.ToArray());

        CryptographicOperations.ZeroMemory(sharedSecret);

        var expiresAt = DateTimeOffset.UtcNow.Add(SessionTtl);

        var session = new SecureSession(
            sessionId, subjectId, command.DeviceId,
            ProtocolVersion, suite,
            c2sKey, s2cKey,
            DateTimeOffset.UtcNow, expiresAt, 0L);

        await store.SaveAsync(session, ct);

        // Server signature: HMAC-SHA256 over sessionId | serverPublicKey | expiresAt
        var sigInput = Encoding.UTF8.GetBytes(
            $"{sessionId:N}:{Base64UrlEncode(serverPublicKeySpki)}:{expiresAt:O}");
        var signature = HMACSHA256.HashData(s2cKey, sigInput);

        return new StartSessionResult(
            sessionId,
            ProtocolVersion,
            suite,
            Base64UrlEncode(serverPublicKeySpki),
            Base64UrlEncode(serverNonceBytes),
            expiresAt,
            Base64UrlEncode(signature));
    }

    public async Task<RenewSessionResult> RenewAsync(
        Guid sessionId, string subjectId, string deviceId, CancellationToken ct)
    {
        var existing = await store.GetAsync(sessionId, ct)
            ?? throw new InvalidOperationException($"Session {sessionId} not found or expired.");

        if (existing.SubjectId != subjectId)
            throw new UnauthorizedAccessException("Session subject mismatch.");

        if (existing.DeviceId != deviceId)
            throw new UnauthorizedAccessException("Session device mismatch.");

        var renewed = existing with { ExpiresAtUtc = DateTimeOffset.UtcNow.Add(SessionTtl) };
        await store.SaveAsync(renewed, ct);

        return new RenewSessionResult(renewed.SessionId, renewed.ExpiresAtUtc);
    }

    public Task RevokeAsync(Guid sessionId, string reason, CancellationToken ct)
        => store.DeleteAsync(sessionId, ct);

    public Task<SecureSession?> GetAsync(Guid sessionId, CancellationToken ct)
        => store.GetAsync(sessionId, ct);

    private static string SelectSuite(IReadOnlyList<string> supported)
    {
        // Prefer ClassicalV1; HybridPqV1 accepted but not yet activated server-side
        if (supported.Contains(SecureSuites.ClassicalV1))
            return SecureSuites.ClassicalV1;

        return supported.FirstOrDefault() ?? SecureSuites.ClassicalV1;
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
