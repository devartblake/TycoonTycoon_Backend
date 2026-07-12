using System.Security.Cryptography;
using Synaptix.Security.Kms.Application.Sessions;

namespace Synaptix.Security.Kms.Tests.Sessions;

// HybridPqV1 (X25519 + ML-KEM-768). These exercise the self-contained HybridKeyExchange
// component; the suite is NOT wired into live negotiation. Tests self-skip on runners whose
// crypto provider lacks ML-KEM so CI stays green regardless of platform support.
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
}
