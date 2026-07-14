using System.Text.RegularExpressions;

namespace Synaptix.Security.Kms.Tests.Contracts;

/// <summary>
/// Guards the Compliance typed client against silent path drift on the Compliance API.
///
/// Server maps: <c>Synaptix.Compliance.Api/Features/Internal/InternalEndpoints.cs</c>.
/// Client paths: <c>Synaptix.Compliance.Client/Http/ComplianceClient.cs</c>.
///
/// Update this catalog whenever either side changes so CI fails until both agree.
/// </summary>
public sealed class ComplianceClientRouteContractTests
{
    /// <summary>
    /// HTTP method + path template for every call site in ComplianceClient.
    /// </summary>
    public static TheoryData<string, string> ExpectedInternalClientRoutes() => new()
    {
        { "GET", "/internal/compliance/users/{userId}/restrictions" },
        { "GET", "/internal/compliance/users/{userId}/consent-status" },
        { "POST", "/internal/compliance/audit" },
        { "POST", "/internal/compliance/parental-consent/initiate" },
        { "GET", "/internal/compliance/privacy-requests/pending" },
        { "PATCH", "/internal/compliance/privacy-requests/{requestId}" },
    };

    [Theory]
    [MemberData(nameof(ExpectedInternalClientRoutes))]
    public void ComplianceClient_source_contains_path_prefix(string method, string pathTemplate)
    {
        var source = ReadClientSource();
        var marker = StaticPathMarker(pathTemplate);

        source.Should().Contain(marker,
            because: $"{method} {pathTemplate} must remain referenced by ComplianceClient (marker '{marker}')");
    }

    [Fact]
    public void ComplianceClient_only_calls_catalogued_internal_paths()
    {
        var source = ReadClientSource();
        var pathLiterals = Regex.Matches(source, @"\$?""(/internal/compliance[^""]*)""")
            .Select(m => NormalizeClientPath(m.Groups[1].Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        pathLiterals.Should().NotBeEmpty();

        var catalog = ExpectedInternalClientRoutes()
            .Select(row => NormalizeClientPath((string)row[1]!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in pathLiterals)
        {
            catalog.Should().Contain(path,
                because: $"client path '{path}' must be listed in ExpectedInternalClientRoutes");
        }
    }

    [Fact]
    public void InternalEndpoints_registers_map_calls_for_catalog()
    {
        var apiSource = File.ReadAllText(FindRepoFile(
            "Synaptix.Compliance.Api", "Features", "Internal", "InternalEndpoints.cs"));

        apiSource.Should().Contain("MapGroup(\"/internal/compliance\")");

        // Relative segments as registered on the group (with optional :guid constraints).
        apiSource.Should().Contain("MapGet(\"/users/{userId:guid}/restrictions\"");
        apiSource.Should().Contain("MapGet(\"/users/{userId:guid}/consent-status\"");
        apiSource.Should().Contain("MapPost(\"/audit\"");
        apiSource.Should().Contain("MapPost(\"/parental-consent/initiate\"");
        apiSource.Should().Contain("MapGet(\"/privacy-requests/pending\"");
        apiSource.Should().Contain("MapPatch(\"/privacy-requests/{requestId:guid}\"");
    }

    private static string StaticPathMarker(string pathTemplate)
    {
        // Longest static prefix before the first template parameter.
        var brace = pathTemplate.IndexOf('{');
        var prefix = brace >= 0 ? pathTemplate[..brace] : pathTemplate;
        return prefix.TrimEnd('/');
    }

    private static string NormalizeClientPath(string raw)
    {
        var p = raw;
        var q = p.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
            p = p[..q];

        p = Regex.Replace(p, @"\{[^}]*userId[^}]*\}", "{userId}", RegexOptions.IgnoreCase);
        p = Regex.Replace(p, @"\{[^}]*requestId[^}]*\}", "{requestId}", RegexOptions.IgnoreCase);
        p = Regex.Replace(p, @"\{[^}]*limit[^}]*\}", "", RegexOptions.IgnoreCase);
        return p.TrimEnd('/');
    }

    private static string ReadClientSource()
        => File.ReadAllText(FindRepoFile("Synaptix.Compliance.Client", "Http", "ComplianceClient.cs"));

    private static string FindRepoFile(params string[] relativeParts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate {string.Join('/', relativeParts)} from {AppContext.BaseDirectory}");
    }
}
