using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Application.Options;
using Synaptix.Security.Kms.Contracts.Suites;

namespace Synaptix.Security.Kms.Application.Sessions;

public sealed class SecureSessionKeyExchange : ISecureSessionKeyExchange
{
    private readonly bool _enableHybridPq;

    private static readonly ECCurve Curve25519 = ECCurve.CreateFromOid(new Oid("1.3.101.110"));

    private static readonly string[] BaseSuitePreference =
    [
        SecureSuites.ClassicalV1,
        SecureSuites.P256V1
    ];

    /// <summary>Defaults to hybrid PQ disabled (safe for unit tests and fallback construction).</summary>
    public SecureSessionKeyExchange()
        : this(enableHybridPq: false)
    {
    }

    public SecureSessionKeyExchange(IOptions<KmsOptions> options)
        : this(options.Value.Suites.EnableHybridPq)
    {
    }

    /// <summary>Explicit flag constructor for tests and controlled construction.</summary>
    public SecureSessionKeyExchange(bool enableHybridPq)
    {
        _enableHybridPq = enableHybridPq;
    }

    private string[] GetSuitePreference() =>
        _enableHybridPq && HybridKeyExchange.IsAvailable
            ? [SecureSuites.HybridPqV1, ..BaseSuitePreference]
            : BaseSuitePreference;

    public bool IsSupported(string suite)
    {
        if (suite == SecureSuites.HybridPqV1)
            return _enableHybridPq && HybridKeyExchange.IsAvailable;

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
        catch (NotSupportedException)
        {
            return false;
        }
    }

    public string SelectSuite(IReadOnlyList<string> supportedSuites)
    {
        var advertised = supportedSuites.Count == 0
            ? GetSuitePreference()
            : GetSuitePreference().Where(supportedSuites.Contains);

        var selected = advertised.FirstOrDefault(IsSupported)
            ?? GetSuitePreference().FirstOrDefault(IsSupported);

        return selected ?? throw new PlatformNotSupportedException(
            "No supported KMS secure session key exchange suite is available on this platform.");
    }

    public ECDiffieHellman CreatePrivateKey(string suite)
        => suite switch
        {
            SecureSuites.ClassicalV1 => ECDiffieHellman.Create(Curve25519),
            SecureSuites.P256V1 => ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256),
            // Hybrid is KEM-shaped (see HybridKeyExchange); SecureSessionService routes
            // HybridPqV1 through HybridKeyExchange.AcceptInitiator, not ECDH helpers.
            SecureSuites.HybridPqV1 => throw new NotSupportedException(
                "HybridPqV1 cannot be created via CreatePrivateKey; use HybridKeyExchange (client initiator / server responder)."),
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
