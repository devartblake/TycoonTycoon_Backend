using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Tycoon.Hosting.Minio;

/// <summary>
/// Provides extension methods for adding MinIO resources to an Aspire distributed application.
/// </summary>
public static class MinioBuilderExtensions
{
    private const int MinioApiPort = 9000;
    private const int MinioConsolePort = 9001;

    /// <summary>
    /// Adds a MinIO container to the application model.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The resource name (used as the container hostname within the Aspire network).</param>
    /// <param name="rootUser">Optional parameter for the root user / access key. Defaults to a generated value.</param>
    /// <param name="rootPassword">Optional parameter for the root password / secret key. Defaults to a generated value.</param>
    /// <param name="apiPort">Optional host port for the S3-compatible API (default: 9000).</param>
    /// <param name="consolePort">Optional host port for the MinIO web console (default: 9001).</param>
    public static IResourceBuilder<MinioResource> AddMinio(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? rootUser = null,
        IResourceBuilder<ParameterResource>? rootPassword = null,
        int? apiPort = null,
        int? consolePort = null)
    {
        var userParam = rootUser?.Resource
            ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-user");

        var passParam = rootPassword?.Resource
            ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var minio = new MinioResource(name, userParam, passParam);

        return builder
            .AddResource(minio)
            .WithImage(MinioContainerImageTags.Image, MinioContainerImageTags.Tag)
            .WithImageRegistry(MinioContainerImageTags.Registry)
            .WithHttpEndpoint(targetPort: MinioApiPort, port: apiPort, name: MinioResource.ApiEndpointName)
            .WithHttpEndpoint(targetPort: MinioConsolePort, port: consolePort, name: MinioResource.ConsoleEndpointName)
            .WithEnvironment(ctx => ctx.EnvironmentVariables["MINIO_ROOT_USER"] = minio.RootUserParameter)
            .WithEnvironment(ctx => ctx.EnvironmentVariables["MINIO_ROOT_PASSWORD"] = minio.RootPasswordParameter)
            .WithArgs("server", "/data", "--console-address", ":9001");
    }

    /// <summary>
    /// Adds a named volume for MinIO data persistence.
    /// </summary>
    public static IResourceBuilder<MinioResource> WithDataVolume(
        this IResourceBuilder<MinioResource> builder,
        string? name = null,
        bool isReadOnly = false) =>
        builder.WithVolume(
            name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"),
            "/data",
            isReadOnly);

    /// <summary>
    /// Mounts a host directory into the MinIO data path.
    /// </summary>
    public static IResourceBuilder<MinioResource> WithDataBindMount(
        this IResourceBuilder<MinioResource> builder,
        string source,
        bool isReadOnly = false) =>
        builder.WithBindMount(source, "/data", isReadOnly);

    /// <summary>
    /// Injects MinIO connection settings as <c>MinIO__*</c> environment variables on a
    /// dependent service, matching the <c>MinioOptions</c> configuration section expected
    /// by <c>Tycoon.Backend.Infrastructure</c>.
    /// </summary>
    /// <param name="builder">The dependent resource builder (e.g. the API project).</param>
    /// <param name="minio">The MinIO resource to connect to.</param>
    /// <param name="bucket">The S3 bucket name (default: <c>tycoon-assets</c>).</param>
    public static IResourceBuilder<TResource> WithMinioConnection<TResource>(
        this IResourceBuilder<TResource> builder,
        IResourceBuilder<MinioResource> minio,
        string bucket = "tycoon-assets")
        where TResource : IResourceWithEnvironment
    {
        return builder
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["MinIO__Endpoint"] =
                    minio.Resource.ApiEndpoint.Property(EndpointProperty.IPV4Host) + ":" +
                    minio.Resource.ApiEndpoint.Property(EndpointProperty.Port);
                ctx.EnvironmentVariables["MinIO__AccessKey"] = minio.Resource.RootUserParameter;
                ctx.EnvironmentVariables["MinIO__SecretKey"] = minio.Resource.RootPasswordParameter;
                ctx.EnvironmentVariables["MinIO__Bucket"] = bucket;
                ctx.EnvironmentVariables["MinIO__UseSSL"] = "false";
                // PublicEndpoint is intentionally omitted so the infrastructure falls back
                // to MinIO__Endpoint (the same host reachable from both server and browser
                // in a local Aspire dev environment).
            });
    }
}

internal static class VolumeNameGenerator
{
    internal static string CreateVolumeName<T>(IResourceBuilder<T> builder, string suffix)
        where T : IResource =>
        $"{builder.ApplicationBuilder.Environment.ApplicationName.ToLowerInvariant().Replace('.', '-')}-{builder.Resource.Name}-{suffix}";
}
