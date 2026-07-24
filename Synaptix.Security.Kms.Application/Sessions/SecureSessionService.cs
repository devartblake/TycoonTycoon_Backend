using System.Security.Cryptography;
using System.Text;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;
using Synaptix.Security.Kms.Contracts.Suites;

namespace Synaptix.Security.Kms.Application.Sessions;

public sealed class SecureSessionService(
    ISessionStore store,
    ISecureSessionKeyExchange keyExchange) : ISecureSessionService
{
    private const string ProtocolVersion = "syn-sec-v1";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(30);

    // Hard ceiling on how long a single secure session may live, regardless of
    // how often it is renewed. Renew extends the 30-min sliding TTL, but a
    // session older than this is retired so a long-lived (or compromised)
    // channel can't be kept alive indefinitely — the client must perform a fresh
    // handshake. Sessions also self-evict from the cache at their sliding TTL;
    // this bounds the *renewable* lifetime, which eviction alone does not.
    private static readonly TimeSpan MaxSessionLifetime = TimeSpan.FromHours(12);

    // Shared hybrid engine for the live session path (server is always the KEM responder).
    private static readonly HybridKeyExchange Hybrid = new();

    public SecureSessionService(ISessionStore store)
        : this(store, new SecureSessionKeyExchange())
    {
    }

    public async Task<StartSessionResult> StartAsync(
        string subjectId, StartSessionCommand command, CancellationToken ct)
    {
        var suite = keyExchange.SelectSuite(command.SupportedSuites);
        var sessionId = Guid.NewGuid();
        var serverNonceBytes = RandomNumberGenerator.GetBytes(24);

        var clientPubKeyBytes = Base64UrlDecode(command.ClientPublicKey);
        var (serverPublicKeyBytes, sharedSecret) = EstablishSharedSecret(suite, clientPubKeyBytes);

        try
        {
            // HKDF-SHA256 key derivation matching the protocol spec (suite-independent after
            // the raw handshake secret is established).
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

            var expiresAt = DateTimeOffset.UtcNow.Add(SessionTtl);

            var session = new SecureSession(
                sessionId, subjectId, command.DeviceId,
                ProtocolVersion, suite,
                c2sKey, s2cKey,
                DateTimeOffset.UtcNow, expiresAt, 0L);

            await store.SaveAsync(session, ct);

            // Server signature: HMAC-SHA256 over the negotiation transcript —
            //   sessionId | serverPublicKey | expiresAt | selectedSuite | advertisedSuites
            // Binding the negotiated suite and the client's advertised suite list lets the
            // client detect a downgrade attack: a MITM stripping strong suites from the
            // advertised list (or forcing a weaker selected suite) yields a signature the
            // client cannot reproduce from what it actually sent/received.
            // NOTE: this binds the transcript into the *signature* only; key derivation is
            // unchanged (wire-compatible). A future hardening step can also mix the transcript
            // into the HKDF salt so tampering makes the AEAD keys diverge, once every client
            // can move to the new derivation in lockstep.
            var advertisedSuites = string.Join('|', command.SupportedSuites);
            var serverPublicKey = Base64UrlEncode(serverPublicKeyBytes);
            var sigInput = Encoding.UTF8.GetBytes(
                $"{sessionId:N}:{serverPublicKey}:{expiresAt:O}:{suite}:{advertisedSuites}");
            var signature = HMACSHA256.HashData(s2cKey, sigInput);

            return new StartSessionResult(
                sessionId,
                ProtocolVersion,
                suite,
                serverPublicKey,
                Base64UrlEncode(serverNonceBytes),
                expiresAt,
                Base64UrlEncode(signature));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sharedSecret);
        }
    }

    /// <summary>
    /// Establish the raw handshake secret for the selected suite.
    /// <list type="bullet">
    /// <item>Classical/P256: ECDH; <paramref name="clientPublicKey"/> is peer SPKI.</item>
    /// <item>
    /// HybridPqV1: client is the hybrid initiator; server is the responder.
    /// <paramref name="clientPublicKey"/> is the initiator bundle
    /// (X25519 SPKI ‖ ML-KEM encapsulation key); the returned server public is the
    /// responder bundle (X25519 SPKI ‖ ML-KEM ciphertext).
    /// </item>
    /// </list>
    /// </summary>
    private (byte[] ServerPublicKey, byte[] SharedSecret) EstablishSharedSecret(
        string suite, byte[] clientPublicKey)
    {
        if (suite == SecureSuites.HybridPqV1)
        {
            var hybrid = Hybrid.AcceptInitiator(clientPublicKey);
            return (hybrid.ResponderPublic, hybrid.SharedSecret);
        }

        using var serverEcdh = keyExchange.CreatePrivateKey(suite);
        var serverPublicKeySpki = keyExchange.ExportPublicKey(serverEcdh);
        var sharedSecret = keyExchange.DeriveSharedSecret(serverEcdh, clientPublicKey);
        return (serverPublicKeySpki, sharedSecret);
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

        // Enforce the absolute lifetime ceiling: retire (delete) sessions that
        // have been alive longer than MaxSessionLifetime instead of renewing them.
        if (DateTimeOffset.UtcNow - existing.CreatedAtUtc >= MaxSessionLifetime)
        {
            await store.DeleteAsync(sessionId, ct);
            throw new InvalidOperationException(
                "Session exceeded maximum lifetime; start a new session.");
        }

        var renewed = existing with { ExpiresAtUtc = DateTimeOffset.UtcNow.Add(SessionTtl) };
        await store.SaveAsync(renewed, ct);

        return new RenewSessionResult(renewed.SessionId, renewed.ExpiresAtUtc);
    }

    public Task RevokeAsync(Guid sessionId, string reason, CancellationToken ct)
        => store.DeleteAsync(sessionId, ct);

    public Task<SecureSession?> GetAsync(Guid sessionId, CancellationToken ct)
        => store.GetAsync(sessionId, ct);

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
