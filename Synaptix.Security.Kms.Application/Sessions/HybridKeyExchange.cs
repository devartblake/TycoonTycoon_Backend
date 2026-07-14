using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Synaptix.Security.Kms.Application.Sessions;

/// <summary>
/// Hybrid post-quantum key exchange for <c>SecureSuites.HybridPqV1</c>
/// (<c>X25519-MLKEM768-HKDF-SHA256-AES256GCM</c>).
///
/// Threat model: harvest-now-decrypt-later. Combining a classical X25519 ECDH with an
/// ML-KEM-768 KEM means an attacker must break BOTH primitives to recover the session
/// key — so recorded traffic stays confidential even if X25519 is later broken by a
/// quantum adversary.
///
/// Live session start uses this when <c>Kms:Suites:EnableHybridPq</c> is true and
/// <see cref="IsAvailable"/>: the client is the initiator and the server is the responder.
/// MUST receive an independent human cryptographic review before production enablement.
///
/// The interface is intentionally KEM-shaped (initiator/responder, encapsulate/decapsulate)
/// rather than the symmetric Diffie–Hellman shape of classical suites, because ML-KEM is a
/// KEM, not a DH primitive.
/// </summary>
public sealed class HybridKeyExchange
{
    private static readonly ECCurve Curve25519 = ECCurve.CreateFromOid(new Oid("1.3.101.110"));

    // Approximate sizes for ML-KEM-768 (NIST Level 3)
    private const int ExpectedX25519SpkiSize = 44;          // Typical for Curve25519 SPKI
    private const int ExpectedMlkemEncapsulationKeySize = 1184;
    private const int ExpectedMlkemCiphertextSize = 1088;
    private const int ExpectedSharedSecretSize = 32;

    private static readonly Lazy<bool> Available = new(DetectAvailable);

    /// <summary>
    /// True when the platform supports both ML-KEM-768 and X25519 (required for HybridPqV1).
    /// </summary>
    public static bool IsAvailable => Available.Value;

    private static bool DetectAvailable()
    {
        if (!MLKem.IsSupported)
            return false;

        try
        {
            using var ecdh = ECDiffieHellman.Create(Curve25519);
            _ = ecdh.ExportSubjectPublicKeyInfo();
            using var kem = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);
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
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Ephemeral secret material held by the initiator between its two round-trip steps.</summary>
    public sealed class InitiatorState : IDisposable
    {
        internal ECDiffieHellman Ecdh { get; init; } = default!;
        internal MLKem Kem { get; init; } = default!;

        public void Dispose()
        {
            Ecdh.Dispose();
            Kem.Dispose();
        }
    }

    /// <summary>Result of the responder accepting an initiator's public bundle.</summary>
    public sealed record ResponderResult(byte[] ResponderPublic, byte[] SharedSecret);

    private static void EnsureAvailable()
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException(
                "HybridPqV1 requires platform support for both X25519 and ML-KEM-768.");
    }

    /// <summary>
    /// Initiator step 1: generate ephemeral X25519 + ML-KEM-768 material and export the
    /// public bundle (X25519 SPKI ‖ ML-KEM encapsulation key) to send to the responder.
    /// </summary>
    public (InitiatorState State, byte[] InitiatorPublic) CreateInitiator()
    {
        EnsureAvailable();

        var ecdh = ECDiffieHellman.Create(Curve25519);
        var kem = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);

        var ecdhSpki = ecdh.ExportSubjectPublicKeyInfo();
        var encapsulationKey = kem.ExportEncapsulationKey();

        var initiatorPublic = Concat(ecdhSpki, encapsulationKey);
        return (new InitiatorState { Ecdh = ecdh, Kem = kem }, initiatorPublic);
    }

    /// <summary>
    /// Responder: consume the initiator's public bundle, derive the shared secret, and
    /// return the responder public bundle (X25519 SPKI ‖ ML-KEM ciphertext) to send back.
    /// </summary>
    public ResponderResult AcceptInitiator(byte[] initiatorPublic)
    {
        EnsureAvailable();

        var (initiatorEcdhSpki, initiatorEncapsulationKey) = Split(initiatorPublic);

        // Quick validation of expected sizes
        ValidateKeySize(initiatorEcdhSpki, ExpectedX25519SpkiSize, "X25519 SPKI");
        ValidateKeySize(initiatorEncapsulationKey, ExpectedMlkemEncapsulationKeySize, "ML-KEM encapsulation key");

        using var responderEcdh = ECDiffieHellman.Create(Curve25519);
        var ecdhSecret = DeriveEcdh(responderEcdh, initiatorEcdhSpki);

        using var peerKem = MLKem.ImportEncapsulationKey(
            MLKemAlgorithm.MLKem768, initiatorEncapsulationKey);
        peerKem.Encapsulate(out var kemCiphertext, out var kemSecret);

        var responderPublic = Concat(responderEcdh.ExportSubjectPublicKeyInfo(), kemCiphertext);
        var shared = CombineSecrets(ecdhSecret, kemSecret);

        CryptographicOperations.ZeroMemory(ecdhSecret);
        CryptographicOperations.ZeroMemory(kemSecret);

        return new ResponderResult(responderPublic, shared);
    }

    /// <summary>
    /// Initiator step 2: consume the responder's public bundle and derive the same shared
    /// secret. The <paramref name="state"/> is not disposed here; the caller owns its lifetime.
    /// </summary>
    public byte[] CompleteInitiator(InitiatorState state, byte[] responderPublic)
    {
        EnsureAvailable();

        var (responderEcdhSpki, kemCiphertext) = Split(responderPublic);

        ValidateKeySize(responderEcdhSpki, ExpectedX25519SpkiSize, "X25519 SPKI");
        ValidateKeySize(kemCiphertext, ExpectedMlkemCiphertextSize, "ML-KEM ciphertext");

        var ecdhSecret = DeriveEcdh(state.Ecdh, responderEcdhSpki);
        var kemSecret = state.Kem.Decapsulate(kemCiphertext);

        var shared = CombineSecrets(ecdhSecret, kemSecret);

        CryptographicOperations.ZeroMemory(ecdhSecret);
        CryptographicOperations.ZeroMemory(kemSecret);

        return shared;
    }

    private static void ValidateKeySize(byte[] key, int expectedSize, string name)
    {
        if (key == null || key.Length != expectedSize)
            throw new CryptographicException($"Invalid {name} size: expected {expectedSize}, got {key?.Length ?? 0}.");
    }

    private static byte[] DeriveEcdh(ECDiffieHellman privateKey, byte[] peerSpki)
    {
        using var peer = ECDiffieHellman.Create();
        peer.ImportSubjectPublicKeyInfo(peerSpki, out _);
        return privateKey.DeriveRawSecretAgreement(peer.PublicKey);
    }

    // Hybrid KEM combiner: HKDF-SHA256 over the concatenation of the classical and PQ
    // secrets. Both must be recovered to derive the session key.
    private static byte[] CombineSecrets(byte[] ecdhSecret, byte[] kemSecret)
    {
        if (ecdhSecret.Length != ExpectedSharedSecretSize || kemSecret.Length != ExpectedSharedSecretSize)
            throw new CryptographicException("Unexpected shared secret lengths in hybrid combiner.");

        var ikm = Concat(ecdhSecret, kemSecret);
        try
        {
            return HKDF.DeriveKey(
                HashAlgorithmName.SHA256, ikm, 32, salt: null,
                "synaptix:hybrid:x25519-mlkem768:v1"u8.ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ikm);
        }
    }

    // Length-prefixed concatenation (4-byte big-endian length per segment) so the two
    // variable-length blobs can be split unambiguously.
    private static byte[] Concat(byte[] first, byte[] second)
    {
        var result = new byte[4 + first.Length + 4 + second.Length];
        var span = result.AsSpan();
        BinaryPrimitives.WriteInt32BigEndian(span, first.Length);
        first.CopyTo(span[4..]);
        BinaryPrimitives.WriteInt32BigEndian(span[(4 + first.Length)..], second.Length);
        second.CopyTo(span[(8 + first.Length)..]);
        return result;
    }

    private static (byte[] First, byte[] Second) Split(byte[] bundle)
    {
        if (bundle == null || bundle.Length < 8)
            throw new CryptographicException("Malformed hybrid key-exchange bundle: too short.");

        var span = bundle.AsSpan();
        var firstLen = BinaryPrimitives.ReadInt32BigEndian(span);
        if (firstLen <= 0 || 4 + firstLen + 4 > bundle.Length)
            throw new CryptographicException("Malformed hybrid key-exchange bundle.");

        var first = span.Slice(4, firstLen).ToArray();
        var secondLen = BinaryPrimitives.ReadInt32BigEndian(span[(4 + firstLen)..]);
        if (secondLen <= 0 || 8 + firstLen + secondLen != bundle.Length)
            throw new CryptographicException("Malformed hybrid key-exchange bundle.");

        var second = span.Slice(8 + firstLen, secondLen).ToArray();
        return (first, second);
    }
}
