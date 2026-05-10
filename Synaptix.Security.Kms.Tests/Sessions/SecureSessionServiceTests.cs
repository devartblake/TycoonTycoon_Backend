using System.Security.Cryptography;
using System.Text;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Application.Sessions;
using Synaptix.Security.Kms.Contracts.Suites;
using Synaptix.Security.Kms.Tests.Helpers;

namespace Synaptix.Security.Kms.Tests.Sessions;

public sealed class SecureSessionServiceTests
{
    // X25519 OID — must match what SecureSessionService uses
    private static readonly ECCurve Curve25519 = ECCurve.CreateFromOid(new Oid("1.3.101.110"));

    private static (string PublicKeyBase64, ECDiffieHellman PrivateKey) GenerateClientKeyPair()
    {
        var ecdh = ECDiffieHellman.Create(Curve25519);
        var spki = ecdh.ExportSubjectPublicKeyInfo();
        return (Base64UrlEncode(spki), ecdh);
    }

    private static string GenerateNonce() => Base64UrlEncode(RandomNumberGenerator.GetBytes(24));

    [Fact]
    public async Task StartAsync_ReturnsNonEmptySessionId_AndClassicalV1Suite()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var (clientPub, clientKey) = GenerateClientKeyPair();
        using var disposable1 = clientKey;

        var result = await svc.StartAsync("user-1", new StartSessionCommand(
            "device-1", GenerateNonce(), clientPub,
            [SecureSuites.ClassicalV1]), default);

        result.SessionId.Should().NotBeEmpty();
        result.ProtocolVersion.Should().Be("syn-sec-v1");
        result.SelectedSuite.Should().Be(SecureSuites.ClassicalV1);
        result.ServerPublicKey.Should().NotBeNullOrEmpty();
        result.ServerNonce.Should().NotBeNullOrEmpty();
        result.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task StartAsync_ClientAndServerDeriveMatchingKeys()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var clientNonceBytes = RandomNumberGenerator.GetBytes(24);
        var clientNonce = Base64UrlEncode(clientNonceBytes);
        var (clientPub, clientEcdh) = GenerateClientKeyPair();
        using var disposable2 = clientEcdh;

        var result = await svc.StartAsync("user-2", new StartSessionCommand(
            "device-2", clientNonce, clientPub,
            [SecureSuites.ClassicalV1]), default);

        // Client-side key derivation (mirrors the protocol spec)
        var serverPubBytes = Base64UrlDecode(result.ServerPublicKey);
        using var serverEcdhImport = ECDiffieHellman.Create();
        serverEcdhImport.ImportSubjectPublicKeyInfo(serverPubBytes, out int _);

        var sharedSecret = clientEcdh.DeriveRawSecretAgreement(serverEcdhImport.PublicKey);
        var serverNonceBytes = Base64UrlDecode(result.ServerNonce);
        var salt = SHA256.HashData(
        [
            ..clientNonceBytes,
            ..serverNonceBytes,
            ..result.SessionId.ToByteArray()
        ]);

        var expectedC2S = HKDF.DeriveKey(
            HashAlgorithmName.SHA256, sharedSecret, 32, salt,
            "synaptix:c2s:v1"u8.ToArray());
        var expectedS2C = HKDF.DeriveKey(
            HashAlgorithmName.SHA256, sharedSecret, 32, salt,
            "synaptix:s2c:v1"u8.ToArray());

        var session = store[result.SessionId];
        session.Should().NotBeNull();
        session!.ClientToServerKey.Should().BeEquivalentTo(expectedC2S);
        session.ServerToClientKey.Should().BeEquivalentTo(expectedS2C);
    }

    [Fact]
    public async Task StartAsync_UnsupportedSuiteOnly_FallsBackToClassicalV1()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var (clientPub, clientKey) = GenerateClientKeyPair();
        using var disposable3 = clientKey;

        var result = await svc.StartAsync("user-3", new StartSessionCommand(
            "device-3", GenerateNonce(), clientPub,
            ["some-unknown-suite"]), default);

        result.SelectedSuite.Should().Be(SecureSuites.ClassicalV1);
    }

    [Fact]
    public async Task StartAsync_InvalidClientPublicKey_Throws()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);

        var act = () => svc.StartAsync("user-4", new StartSessionCommand(
            "device-4", GenerateNonce(), "not-a-valid-key",
            [SecureSuites.ClassicalV1]), default);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task RenewAsync_ExtendsExpiry_SameSessionId()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var (clientPub, clientKey) = GenerateClientKeyPair();
        using var disposable5 = clientKey;

        var start = await svc.StartAsync("user-5", new StartSessionCommand(
            "device-5", GenerateNonce(), clientPub,
            [SecureSuites.ClassicalV1]), default);

        var renewed = await svc.RenewAsync(start.SessionId, "user-5", "device-5", default);

        renewed.SessionId.Should().Be(start.SessionId);
        renewed.ExpiresAtUtc.Should().BeAfter(start.ExpiresAtUtc.AddMinutes(-1));
    }

    [Fact]
    public async Task RenewAsync_WrongDevice_Throws()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var (clientPub, clientKey) = GenerateClientKeyPair();
        using var disposable6 = clientKey;

        var start = await svc.StartAsync("user-6", new StartSessionCommand(
            "device-6", GenerateNonce(), clientPub,
            [SecureSuites.ClassicalV1]), default);

        var act = () => svc.RenewAsync(start.SessionId, "user-6", "wrong-device", default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RevokeAsync_RemovesSession_GetReturnsNull()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var (clientPub, clientKey) = GenerateClientKeyPair();
        using var disposable7 = clientKey;

        var start = await svc.StartAsync("user-7", new StartSessionCommand(
            "device-7", GenerateNonce(), clientPub,
            [SecureSuites.ClassicalV1]), default);

        await svc.RevokeAsync(start.SessionId, "manual-test", default);

        var session = await svc.GetAsync(start.SessionId, default);
        session.Should().BeNull();
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch { 2 => padded + "==", 3 => padded + "=", _ => padded };
        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
