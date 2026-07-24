using System.Text;
using FluentAssertions;
using Synaptix.Security.Kms.Client.Security;

namespace Synaptix.Backend.Api.Tests.Security;

public sealed class KmsCertificatePinningTests
{
    private static readonly byte[] LeafDer = Encoding.UTF8.GetBytes("kms-leaf-cert-der");

    [Fact]
    public void IsPinned_MatchesWhenFingerprintConfigured()
    {
        var pin = KmsCertificatePinning.ComputePin(LeafDer);
        KmsCertificatePinning.IsPinned(LeafDer, new[] { pin }).Should().BeTrue();
    }

    [Fact]
    public void IsPinned_IgnoresWhitespaceAroundPins()
    {
        var pin = KmsCertificatePinning.ComputePin(LeafDer);
        KmsCertificatePinning.IsPinned(LeafDer, new[] { "  " + pin + "  " }).Should().BeTrue();
    }

    [Fact]
    public void IsPinned_RejectsNonMatchingCertificate()
    {
        var pin = KmsCertificatePinning.ComputePin(LeafDer);
        var other = Encoding.UTF8.GetBytes("a-different-cert");
        KmsCertificatePinning.IsPinned(other, new[] { pin }).Should().BeFalse();
    }

    [Fact]
    public void IsPinned_RejectsWhenNoPinsConfigured()
    {
        KmsCertificatePinning.IsPinned(LeafDer, null).Should().BeFalse();
        KmsCertificatePinning.IsPinned(LeafDer, Array.Empty<string>()).Should().BeFalse();
    }

    [Fact]
    public void IsPinned_SupportsMultiplePins_ForRotation()
    {
        var nextDer = Encoding.UTF8.GetBytes("next-rotation-cert");
        var pins = new[]
        {
            KmsCertificatePinning.ComputePin(LeafDer),
            KmsCertificatePinning.ComputePin(nextDer),
        };
        KmsCertificatePinning.IsPinned(nextDer, pins).Should().BeTrue();
    }
}
