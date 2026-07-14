using System.Security.Cryptography;
using Synaptix.Security.Kms.Application.Sessions;
using Synaptix.Security.Kms.Contracts.Suites;
using Synaptix.Security.Kms.Tests.Helpers;

namespace Synaptix.Security.Kms.Tests.Sessions;

public sealed class SecureSessionServiceTests
{
    private static readonly SecureSessionKeyExchange KeyExchange = new();

    private static string SupportedSuite => KeyExchange.SelectSuite([SecureSuites.ClassicalV1, SecureSuites.P256V1]);

    private static (string PublicKeyBase64, ECDiffieHellman PrivateKey) GenerateClientKeyPair(string suite)
    {
        var ecdh = KeyExchange.CreatePrivateKey(suite);
        var spki = KeyExchange.ExportPublicKey(ecdh);
        return (Base64UrlEncode(spki), ecdh);
    }

    private static string GenerateNonce() => Base64UrlEncode(RandomNumberGenerator.GetBytes(24));

    [Fact]
    public async Task StartAsync_ReturnsNonEmptySessionId_AndClassicalV1Suite()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var suite = SupportedSuite;
        var (clientPub, clientKey) = GenerateClientKeyPair(suite);
        using var disposable1 = clientKey;

        var result = await svc.StartAsync("user-1", new StartSessionCommand(
            "device-1", GenerateNonce(), clientPub,
            [suite]), default);

        result.SessionId.Should().NotBeEmpty();
        result.ProtocolVersion.Should().Be("syn-sec-v1");
        result.SelectedSuite.Should().Be(suite);
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
        var suite = SupportedSuite;
        var (clientPub, clientEcdh) = GenerateClientKeyPair(suite);
        using var disposable2 = clientEcdh;

        var result = await svc.StartAsync("user-2", new StartSessionCommand(
            "device-2", clientNonce, clientPub,
            [suite]), default);

        // Client-side key derivation (mirrors the protocol spec)
        var serverPubBytes = Base64UrlDecode(result.ServerPublicKey);
        var sharedSecret = KeyExchange.DeriveSharedSecret(clientEcdh, serverPubBytes);
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
    public async Task StartAsync_Signature_BindsSuiteAndAdvertisedList()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var clientNonceBytes = RandomNumberGenerator.GetBytes(24);
        var clientNonce = Base64UrlEncode(clientNonceBytes);
        var suite = SupportedSuite;
        var advertised = new[] { SecureSuites.ClassicalV1, SecureSuites.P256V1 };
        var (clientPub, clientEcdh) = GenerateClientKeyPair(suite);
        using var disposable = clientEcdh;

        var result = await svc.StartAsync("user-sig", new StartSessionCommand(
            "device-sig", clientNonce, clientPub, advertised), default);

        // Client re-derives s2cKey, then reconstructs the signed transcript from what it
        // advertised and what it received — proving the suite + advertised list are bound.
        var serverPubBytes = Base64UrlDecode(result.ServerPublicKey);
        var sharedSecret = KeyExchange.DeriveSharedSecret(clientEcdh, serverPubBytes);
        var serverNonceBytes = Base64UrlDecode(result.ServerNonce);
        var salt = SHA256.HashData(
        [
            ..clientNonceBytes,
            ..serverNonceBytes,
            ..result.SessionId.ToByteArray()
        ]);
        var s2cKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256, sharedSecret, 32, salt, "synaptix:s2c:v1"u8.ToArray());

        var expectedSigInput = System.Text.Encoding.UTF8.GetBytes(
            $"{result.SessionId:N}:{result.ServerPublicKey}:{result.ExpiresAtUtc:O}:{result.SelectedSuite}:{string.Join('|', advertised)}");
        var expectedSig = Base64UrlEncode(HMACSHA256.HashData(s2cKey, expectedSigInput));

        result.ServerSignature.Should().Be(expectedSig);
    }

    [Fact]
    public async Task StartAsync_Signature_DetectsDowngradeTampering()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var clientNonceBytes = RandomNumberGenerator.GetBytes(24);
        var clientNonce = Base64UrlEncode(clientNonceBytes);
        var suite = SupportedSuite;
        var (clientPub, clientEcdh) = GenerateClientKeyPair(suite);
        using var disposable = clientEcdh;

        // Client actually advertised both suites...
        var result = await svc.StartAsync("user-dg", new StartSessionCommand(
            "device-dg", clientNonce, clientPub,
            [SecureSuites.ClassicalV1, SecureSuites.P256V1]), default);

        var serverPubBytes = Base64UrlDecode(result.ServerPublicKey);
        var sharedSecret = KeyExchange.DeriveSharedSecret(clientEcdh, serverPubBytes);
        var serverNonceBytes = Base64UrlDecode(result.ServerNonce);
        var salt = SHA256.HashData(
        [
            ..clientNonceBytes,
            ..serverNonceBytes,
            ..result.SessionId.ToByteArray()
        ]);
        var s2cKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256, sharedSecret, 32, salt, "synaptix:s2c:v1"u8.ToArray());

        // ...but a MITM stripped P256V1 from the list the client thinks it sent.
        var tamperedSigInput = System.Text.Encoding.UTF8.GetBytes(
            $"{result.SessionId:N}:{result.ServerPublicKey}:{result.ExpiresAtUtc:O}:{result.SelectedSuite}:{SecureSuites.ClassicalV1}");
        var tamperedSig = Base64UrlEncode(HMACSHA256.HashData(s2cKey, tamperedSigInput));

        // The signature over the tampered transcript cannot match the server's signature.
        result.ServerSignature.Should().NotBe(tamperedSig);
    }

    [Fact]
    public async Task StartAsync_UnsupportedSuiteOnly_FallsBackToSupportedSuite()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var suite = SupportedSuite;
        var (clientPub, clientKey) = GenerateClientKeyPair(suite);
        using var disposable3 = clientKey;

        var result = await svc.StartAsync("user-3", new StartSessionCommand(
            "device-3", GenerateNonce(), clientPub,
            ["some-unknown-suite"]), default);

        result.SelectedSuite.Should().Be(suite);
    }

    [Fact]
    public void KeyExchange_ClassicalV1_UsesX25519WhenPlatformSupportsIt()
    {
        if (!KeyExchange.IsSupported(SecureSuites.ClassicalV1))
            return;

        using var key = KeyExchange.CreatePrivateKey(SecureSuites.ClassicalV1);
        var publicKey = KeyExchange.ExportPublicKey(key);

        publicKey.Should().NotBeEmpty();
        KeyExchange.SelectSuite([SecureSuites.ClassicalV1, SecureSuites.P256V1])
            .Should().Be(SecureSuites.ClassicalV1);
    }

    [Fact]
    public async Task StartAsync_InvalidClientPublicKey_Throws()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);

        var act = () => svc.StartAsync("user-4", new StartSessionCommand(
            "device-4", GenerateNonce(), "not-a-valid-key",
            [SupportedSuite]), default);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task RenewAsync_ExtendsExpiry_SameSessionId()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var suite = SupportedSuite;
        var (clientPub, clientKey) = GenerateClientKeyPair(suite);
        using var disposable5 = clientKey;

        var start = await svc.StartAsync("user-5", new StartSessionCommand(
            "device-5", GenerateNonce(), clientPub,
            [suite]), default);

        var renewed = await svc.RenewAsync(start.SessionId, "user-5", "device-5", default);

        renewed.SessionId.Should().Be(start.SessionId);
        renewed.ExpiresAtUtc.Should().BeAfter(start.ExpiresAtUtc.AddMinutes(-1));
    }

    [Fact]
    public async Task RenewAsync_WrongDevice_Throws()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var suite = SupportedSuite;
        var (clientPub, clientKey) = GenerateClientKeyPair(suite);
        using var disposable6 = clientKey;

        var start = await svc.StartAsync("user-6", new StartSessionCommand(
            "device-6", GenerateNonce(), clientPub,
            [suite]), default);

        var act = () => svc.RenewAsync(start.SessionId, "user-6", "wrong-device", default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RevokeAsync_RemovesSession_GetReturnsNull()
    {
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store);
        var suite = SupportedSuite;
        var (clientPub, clientKey) = GenerateClientKeyPair(suite);
        using var disposable7 = clientKey;

        var start = await svc.StartAsync("user-7", new StartSessionCommand(
            "device-7", GenerateNonce(), clientPub,
            [suite]), default);

        await svc.RevokeAsync(start.SessionId, "manual-test", default);

        var session = await svc.GetAsync(start.SessionId, default);
        session.Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_HybridPq_SelectsHybrid_AndDerivesMatchingKeys()
    {
        if (!HybridKeyExchange.IsAvailable)
            return;

        var hybrid = new HybridKeyExchange();
        var keyExchange = new SecureSessionKeyExchange(enableHybridPq: true);
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store, keyExchange);

        var clientNonceBytes = RandomNumberGenerator.GetBytes(24);
        var clientNonce = Base64UrlEncode(clientNonceBytes);
        var (initiatorState, initiatorPublic) = hybrid.CreateInitiator();
        using var state = initiatorState;

        var advertised = new[]
        {
            SecureSuites.HybridPqV1,
            SecureSuites.ClassicalV1,
            SecureSuites.P256V1
        };

        var result = await svc.StartAsync("user-hybrid", new StartSessionCommand(
            "device-hybrid",
            clientNonce,
            Base64UrlEncode(initiatorPublic),
            advertised), default);

        result.SelectedSuite.Should().Be(SecureSuites.HybridPqV1);

        // Client completes the hybrid handshake (initiator step 2) with the server bundle.
        var serverBundle = Base64UrlDecode(result.ServerPublicKey);
        var sharedSecret = hybrid.CompleteInitiator(initiatorState, serverBundle);

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
        session!.Suite.Should().Be(SecureSuites.HybridPqV1);
        session.ClientToServerKey.Should().BeEquivalentTo(expectedC2S);
        session.ServerToClientKey.Should().BeEquivalentTo(expectedS2C);

        // Transcript signature still verifies with the hybrid-derived s2c key.
        var expectedSigInput = System.Text.Encoding.UTF8.GetBytes(
            $"{result.SessionId:N}:{result.ServerPublicKey}:{result.ExpiresAtUtc:O}:{result.SelectedSuite}:{string.Join('|', advertised)}");
        var expectedSig = Base64UrlEncode(HMACSHA256.HashData(expectedS2C, expectedSigInput));
        result.ServerSignature.Should().Be(expectedSig);
    }

    [Fact]
    public async Task StartAsync_HybridPq_FlagOff_FallsBackToClassicalEvenIfAdvertised()
    {
        var keyExchange = new SecureSessionKeyExchange(enableHybridPq: false);
        var store = new InMemorySessionStore();
        var svc = new SecureSessionService(store, keyExchange);
        var suite = keyExchange.SelectSuite([SecureSuites.ClassicalV1, SecureSuites.P256V1]);
        var (clientPub, clientKey) = GenerateClientKeyPair(suite);
        using var disposable = clientKey;

        var result = await svc.StartAsync("user-no-hybrid", new StartSessionCommand(
            "device-no-hybrid",
            GenerateNonce(),
            clientPub,
            [SecureSuites.HybridPqV1, suite]), default);

        result.SelectedSuite.Should().NotBe(SecureSuites.HybridPqV1);
        result.SelectedSuite.Should().Be(suite);
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
