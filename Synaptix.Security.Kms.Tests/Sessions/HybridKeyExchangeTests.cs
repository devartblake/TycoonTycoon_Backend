using System.Security.Cryptography;
using Synaptix.Security.Kms.Application.Sessions;

namespace Synaptix.Security.Kms.Tests.Sessions;

// HybridPqV1 (X25519 + ML-KEM-768). Exercises HybridKeyExchange used by live session start
// when EnableHybridPq is on. Tests self-skip when IsAvailable is false (missing ML-KEM
// and/or X25519) so CI stays green regardless of platform support.
public sealed class HybridKeyExchangeTests
{
    private static readonly HybridKeyExchange Exchange = new();

    [Fact]
    public void InitiatorAndResponder_DeriveEqualSharedSecret()
    {
        if (!HybridKeyExchange.IsAvailable)
            return; // ML-KEM unsupported on this platform.

        var (state, initiatorPublic) = Exchange.CreateInitiator();
        using var disposable = state;

        var responder = Exchange.AcceptInitiator(initiatorPublic);
        var initiatorSecret = Exchange.CompleteInitiator(state, responder.ResponderPublic);

        // The whole point of the hybrid handshake: both parties independently derive the
        // same 32-byte key from the classical + PQ secrets.
        initiatorSecret.Should().HaveCount(32);
        initiatorSecret.Should().BeEquivalentTo(responder.SharedSecret);
    }

    [Fact]
    public void DistinctHandshakes_ProduceDistinctSecrets()
    {
        if (!HybridKeyExchange.IsAvailable)
            return;

        var (state1, pub1) = Exchange.CreateInitiator();
        using var d1 = state1;
        var (state2, pub2) = Exchange.CreateInitiator();
        using var d2 = state2;

        var secret1 = Exchange.CompleteInitiator(state1, Exchange.AcceptInitiator(pub1).ResponderPublic);
        var secret2 = Exchange.CompleteInitiator(state2, Exchange.AcceptInitiator(pub2).ResponderPublic);

        // Ephemeral keys per handshake => independent session secrets.
        secret1.Should().NotBeEquivalentTo(secret2);
    }

    [Fact]
    public void AcceptInitiator_MalformedBundle_Throws()
    {
        if (!HybridKeyExchange.IsAvailable)
            return;

        var act = () => Exchange.AcceptInitiator([0x00, 0x00, 0x00, 0x7F]); // claims 127-byte segment
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void WrongSizeX25519Spki_Throws()
    {
        if (!HybridKeyExchange.IsAvailable)
            return;

        // Create a valid initiator bundle but tamper to trigger size validation
        var (state, initiatorPublic) = Exchange.CreateInitiator();
        using var _ = state;

        // Create a bundle with wrong first segment size (simulate wrong X25519 SPKI)
        var corrupted = new byte[100]; // too small / wrong structure
        // Copy some data but keep invalid length prefix
        BitConverter.GetBytes(10).Reverse().ToArray().CopyTo(corrupted, 0); // bogus first length

        var act = () => Exchange.AcceptInitiator(corrupted);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void WrongSizeMlkemEncapsulationKey_Throws()
    {
        if (!HybridKeyExchange.IsAvailable)
            return;

        var (state, initiatorPublic) = Exchange.CreateInitiator();
        using var _ = state;

        // Tamper the bundle so second segment has wrong length
        var firstLen = BitConverter.ToInt32(initiatorPublic.Take(4).Reverse().ToArray(), 0);
        var corrupted = new byte[initiatorPublic.Length];
        initiatorPublic.CopyTo(corrupted, 0);

        // Overwrite second length with invalid value
        var secondLenPos = 4 + firstLen;
        BitConverter.GetBytes(50).Reverse().ToArray().CopyTo(corrupted, secondLenPos);

        var act = () => Exchange.AcceptInitiator(corrupted);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void WrongSizeMlkemCiphertext_Throws()
    {
        if (!HybridKeyExchange.IsAvailable)
            return;

        var (state, initiatorPublic) = Exchange.CreateInitiator();
        using var _ = state;

        var responder = Exchange.AcceptInitiator(initiatorPublic);

        // Corrupt the responder bundle's ciphertext segment length
        var firstLen = BitConverter.ToInt32(responder.ResponderPublic.Take(4).Reverse().ToArray(), 0);
        var corrupted = new byte[responder.ResponderPublic.Length];
        responder.ResponderPublic.CopyTo(corrupted, 0);

        var secondLenPos = 4 + firstLen;
        BitConverter.GetBytes(100).Reverse().ToArray().CopyTo(corrupted, secondLenPos);

        var act = () => Exchange.CompleteInitiator(state, corrupted);
        act.Should().Throw<CryptographicException>();
    }
}
