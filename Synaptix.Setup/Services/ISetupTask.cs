using Microsoft.Extensions.Configuration;

namespace Synaptix.Setup.Services;

public interface ISetupTask
{
    string Name { get; }
    Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default);
}

public sealed class SetupResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public static SetupResult Ok(string message, IReadOnlyList<string>? warnings = null) =>
        new() { Success = true, Message = message, Warnings = warnings ?? [] };

    public static SetupResult Fail(string error) =>
        new() { Success = false, Error = error };

    public static SetupResult Skip(string reason) =>
        new() { Success = true, Message = $"Skipped: {reason}" };
}
