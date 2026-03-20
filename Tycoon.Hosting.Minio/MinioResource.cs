using Aspire.Hosting.ApplicationModel;

namespace Tycoon.Hosting.Minio;

/// <summary>
/// Represents a MinIO object-storage container resource in the Aspire application model.
/// </summary>
public sealed class MinioResource(string name, ParameterResource rootUser, ParameterResource rootPassword)
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string ApiEndpointName = "api";
    internal const string ConsoleEndpointName = "console";

    /// <summary>The parameter that holds the MinIO root user (access key).</summary>
    public ParameterResource RootUserParameter { get; } = rootUser;

    /// <summary>The parameter that holds the MinIO root password (secret key).</summary>
    public ParameterResource RootPasswordParameter { get; } = rootPassword;

    private EndpointReference? _apiEndpoint;
    private EndpointReference? _consoleEndpoint;

    /// <summary>The HTTP API endpoint (port 9000).</summary>
    public EndpointReference ApiEndpoint =>
        _apiEndpoint ??= new EndpointReference(this, ApiEndpointName);

    /// <summary>The MinIO web console endpoint (port 9001).</summary>
    public EndpointReference ConsoleEndpoint =>
        _consoleEndpoint ??= new EndpointReference(this, ConsoleEndpointName);

    /// <summary>
    /// Connection string in the form used by the MinIO .NET SDK:
    /// <c>http://&lt;user&gt;:&lt;password&gt;@&lt;host&gt;:&lt;port&gt;</c>
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"http://{RootUserParameter}:{RootPasswordParameter}@{ApiEndpoint.Property(EndpointProperty.Host)}:{ApiEndpoint.Property(EndpointProperty.Port)}");
}
