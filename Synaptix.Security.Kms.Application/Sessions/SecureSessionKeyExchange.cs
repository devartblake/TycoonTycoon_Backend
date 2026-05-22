using System.Security.Cryptography;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Suites;

namespace Synaptix.Security.Kms.Application.Sessions;

public sealed class SecureSessionKeyExchange : ISecureSessionKeyExchange
{
    private static readonly ECCurve Curve25519 = ECCurve.CreateFromOid(new Oid("1.3.101.110"));
    private static readonly string[] SuitePreference =
    [
        SecureSuites.ClassicalV1,
        SecureSuites.P256V1
    ];

    public bool IsSupported(string suite)
    {
        try
        {
            using var key = CreatePrivateKey(suite);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public string SelectSuite(IReadOnlyList<string> supportedSuites)
    {
        var advertised = supportedSuites.Count == 0
            ? SuitePreference
            : SuitePreference.Where(supportedSuites.Contains);

        var selected = advertised.FirstOrDefault(IsSupported)
            ?? SuitePreference.FirstOrDefault(IsSupported);

        return selected ?? throw new PlatformNotSupportedException(
            "No supported KMS secure session key exchange suite is available on this platform.");
    }

    public ECDiffieHellman CreatePrivateKey(string suite)
        => suite switch
        {
            SecureSuites.ClassicalV1 => ECDiffieHellman.Create(Curve25519),
            SecureSuites.P256V1 => ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256),
            _ => throw new NotSupportedException($"Secure session suite '{suite}' is not supported.")
        };

    public byte[] ExportPublicKey(ECDiffieHellman privateKey)
        => privateKey.ExportSubjectPublicKeyInfo();

    public byte[] DeriveSharedSecret(ECDiffieHellman privateKey, byte[] peerPublicKey)
    {
        using var peer = ECDiffieHellman.Create();
        peer.ImportSubjectPublicKeyInfo(peerPublicKey, out _);
        return privateKey.DeriveRawSecretAgreement(peer.PublicKey);
    }
}
