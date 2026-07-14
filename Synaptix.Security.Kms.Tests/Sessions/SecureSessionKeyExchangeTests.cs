using Microsoft.Extensions.Options;
using Synaptix.Security.Kms.Application.Options;
using Synaptix.Security.Kms.Application.Sessions;
using Synaptix.Security.Kms.Contracts.Suites;

namespace Synaptix.Security.Kms.Tests.Sessions;

public sealed class SecureSessionKeyExchangeTests
{
    [Fact]
    public void IsSupported_HybridPq_IsFalse_WhenFlagDisabled()
    {
        var exchange = new SecureSessionKeyExchange(enableHybridPq: false);

        exchange.IsSupported(SecureSuites.HybridPqV1).Should().BeFalse();
    }

    [Fact]
    public void SelectSuite_NeverSelectsHybrid_WhenFlagDisabled()
    {
        var exchange = new SecureSessionKeyExchange(enableHybridPq: false);

        var selected = exchange.SelectSuite(
        [
            SecureSuites.HybridPqV1,
            SecureSuites.ClassicalV1,
            SecureSuites.P256V1
        ]);

        selected.Should().NotBe(SecureSuites.HybridPqV1);
        selected.Should().BeOneOf(SecureSuites.ClassicalV1, SecureSuites.P256V1);
    }

    [Fact]
    public void IsSupported_HybridPq_RequiresFlagAndPlatformSupport()
    {
        var exchange = new SecureSessionKeyExchange(enableHybridPq: true);

        exchange.IsSupported(SecureSuites.HybridPqV1)
            .Should().Be(HybridKeyExchange.IsAvailable);
    }

    [Fact]
    public void SelectSuite_PrefersHybrid_WhenFlagEnabledAndPlatformSupportsIt()
    {
        if (!HybridKeyExchange.IsAvailable)
            return; // platform without ML-KEM: preference falls through to classical

        var exchange = new SecureSessionKeyExchange(enableHybridPq: true);

        var selected = exchange.SelectSuite(
        [
            SecureSuites.HybridPqV1,
            SecureSuites.ClassicalV1,
            SecureSuites.P256V1
        ]);

        selected.Should().Be(SecureSuites.HybridPqV1);
    }

    [Fact]
    public void OptionsBinding_DefaultsEnableHybridPqToFalse()
    {
        var options = Options.Create(new KmsOptions());
        var exchange = new SecureSessionKeyExchange(options);

        exchange.IsSupported(SecureSuites.HybridPqV1).Should().BeFalse();
    }

    [Fact]
    public void CreatePrivateKey_HybridPq_ThrowsNotSupported()
    {
        var exchange = new SecureSessionKeyExchange(enableHybridPq: true);

        var act = () => exchange.CreatePrivateKey(SecureSuites.HybridPqV1);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*HybridKeyExchange*");
    }

    [Fact]
    public void SelectSuite_WithHybridEnabled_FallsBackWhenHybridUnavailable()
    {
        // When hybrid is unavailable on the platform, preference still falls to classical.
        var exchange = new SecureSessionKeyExchange(enableHybridPq: true);
        var selected = exchange.SelectSuite(
        [
            SecureSuites.HybridPqV1,
            SecureSuites.ClassicalV1,
            SecureSuites.P256V1
        ]);

        if (HybridKeyExchange.IsAvailable)
            selected.Should().Be(SecureSuites.HybridPqV1);
        else
            selected.Should().BeOneOf(SecureSuites.ClassicalV1, SecureSuites.P256V1);
    }
}
