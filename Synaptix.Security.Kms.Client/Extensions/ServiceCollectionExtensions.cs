using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Security.Kms.Client.Http;
using Synaptix.Security.Kms.Client.Options;
using Synaptix.Security.Kms.Client.Security;

namespace Synaptix.Security.Kms.Client.Extensions;

public static class ServiceCollectionExtensions
{
    /// Registers all KMS typed clients and binds KmsClientOptions from configuration.
    /// Callers add a single line: services.AddKmsClient(configuration)
    public static IServiceCollection AddKmsClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<KmsClientOptions>()
            .Bind(configuration.GetSection(KmsClientOptions.SectionName))
            .ValidateOnStart();

        services
            .AddHttpClient<IKmsSessionClient, KmsSessionClient>()
            .ConfigureKmsHttpClient(configuration)
            .ConfigureKmsCertificatePinning()
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsPayloadClient, KmsPayloadClient>()
            .ConfigureKmsHttpClient(configuration)
            .ConfigureKmsCertificatePinning()
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsKeyClient, KmsKeyClient>()
            .ConfigureKmsHttpClient(configuration)
            .ConfigureKmsCertificatePinning()
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IKmsInternalClient, KmsInternalClient>()
            .ConfigureKmsHttpClient(configuration)
            .ConfigureKmsCertificatePinning()
            .AddStandardResilienceHandler();

        return services;
    }

    private static IHttpClientBuilder ConfigureKmsHttpClient(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        return builder.ConfigureHttpClient((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<KmsClientOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);

            if (!string.IsNullOrEmpty(opts.ServiceToken))
                client.DefaultRequestHeaders.Add("X-Service-Token", opts.ServiceToken);
        });
    }

    /// Pins TLS to the KMS host: when KmsClient:PinningEnabled is true, the
    /// presented leaf certificate's SHA-256 must be in PinnedCertificatesSha256,
    /// otherwise the connection is refused. When disabled, standard chain
    /// validation applies unchanged.
    private static IHttpClientBuilder ConfigureKmsCertificatePinning(
        this IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<KmsClientOptions>>().Value;
            var handler = new SocketsHttpHandler();

            if (!opts.PinningEnabled || opts.PinnedCertificatesSha256.Length == 0)
                return handler;

            var logger = sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Synaptix.Security.Kms.Client.CertificatePinning");
            var pins = opts.PinnedCertificatesSha256;

            handler.SslOptions.RemoteCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (certificate is null)
                    {
                        logger.LogError("KMS TLS pinning: no server certificate presented.");
                        return false;
                    }

                    if (KmsCertificatePinning.IsPinned(certificate.GetRawCertData(), pins))
                        return true;

                    logger.LogError(
                        "KMS TLS pinning: leaf certificate {Fingerprint} did not match any configured pin (sslPolicyErrors={Errors}).",
                        KmsCertificatePinning.ComputePin(certificate.GetRawCertData()), sslPolicyErrors);
                    return false;
                };

            return handler;
        });
    }
}
