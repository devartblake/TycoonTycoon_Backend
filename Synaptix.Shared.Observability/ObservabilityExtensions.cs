using System;
using Microsoft.AspNetCore.Builder;                  // WebApplicationBuilder
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Synaptix.Shared.Observability
{
    /// <summary>
    /// One-call observability registration for API + workers.
    /// - Serilog console logging
    /// - OpenTelemetry tracing + metrics
    /// - Optional OTLP exporter when OTEL_EXPORTER_OTLP_ENDPOINT or Observability:OtlpEndpoint is set
    /// </summary>
    public static class ObservabilityExtensions
    {
        // -----------------------------
        // API HOST (WebApplicationBuilder)
        // -----------------------------
        public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, string? serviceName = null)
        {
            // Keep your original convention; adjust if your SynaptixObservability differs.
            serviceName ??= builder.Configuration["Observability:ServiceName"]
                         ?? SynaptixObservability.ServiceName;

            ConfigureSerilog(builder.Configuration);

            // Add Serilog to ASP.NET Core logging pipeline
            builder.Host.UseSerilog((ctx, cfg) =>
            {
                cfg.ReadFrom.Configuration(ctx.Configuration);
                cfg.WriteTo.Console();
            });

            AddOpenTelemetryCore(builder.Services, builder.Configuration, serviceName);

            return builder;
        }

        // -----------------------------
        // GENERIC HOST (Host.CreateDefaultBuilder)
        // -----------------------------
        public static IServiceCollection AddObservability(
            this IServiceCollection services,
            IConfiguration configuration,
            string? serviceName = null)
        {
            serviceName ??= configuration["Observability:ServiceName"]
                         ?? SynaptixObservability.ServiceName;

            // For Generic Host, you typically call .UseSerilog(...) on the host builder itself.
            // This ensures Log.Logger exists for anything that logs early.
            ConfigureSerilog(configuration);

            AddOpenTelemetryCore(services, configuration, serviceName);

            return services;
        }

        // -----------------------------
        // Shared internals
        // -----------------------------
        private static void ConfigureSerilog(IConfiguration configuration)
        {
            // Safe to call multiple times; keeps Log.Logger initialized.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .WriteTo.Console()
                .CreateLogger();
        }

        private static void AddOpenTelemetryCore(IServiceCollection services, IConfiguration configuration, string serviceName)
        {
            var resource = ResourceBuilder.CreateDefault().AddService(serviceName);

            var otlpEndpoint =
                configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                ?? configuration["Observability:OtlpEndpoint"];

            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService(serviceName))
                .WithTracing(t =>
                {
                    t.SetResourceBuilder(resource)
                     .AddSource(SynaptixObservability.ActivitySourceName)
                     .AddAspNetCoreInstrumentation()
                     .AddHttpClientInstrumentation();

                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                        t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                })
                .WithMetrics(m =>
                {
                    m.SetResourceBuilder(resource)
                     .AddMeter(SynaptixObservability.MeterName)
                     .AddAspNetCoreInstrumentation()
                     .AddHttpClientInstrumentation()
                     .AddRuntimeInstrumentation()
                     .AddProcessInstrumentation();

                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                        m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                });
        }
    }
}
