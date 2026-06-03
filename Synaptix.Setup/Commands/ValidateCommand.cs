using Microsoft.Extensions.Configuration;
using Serilog;
using Synaptix.Setup.Security;

namespace Synaptix.Setup.Commands;

public static class ValidateCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        var isLocal = args.Contains("--local") ||
                      (cfg["ASPNETCORE_ENVIRONMENT"] ?? "").Equals("Development", StringComparison.OrdinalIgnoreCase);
        var strict  = args.Contains("--strict");

        var mode = Enum.TryParse<SetupSecretProtectionMode>(
            cfg[$"{SetupSecretOptions.SectionKey}:ProtectionMode"], ignoreCase: true, out var m)
            ? m : SetupSecretProtectionMode.PlaintextLocal;

        Log.Information("=== Synaptix.Setup validate (local={IsLocal}, strict={Strict}, protectionMode={Mode}) ===",
            isLocal, strict, mode);

        var options = new SetupSecretOptions
        {
            ProtectionMode  = mode,
            KmsBaseUrl      = cfg[$"{SetupSecretOptions.SectionKey}:KmsBaseUrl"] ?? cfg["KMS_API_BASE_URL"],
            KmsServiceToken = cfg[$"{SetupSecretOptions.SectionKey}:KmsServiceToken"] ?? cfg["KMS_SERVICE_TOKEN"],
        };

        var validator = new SetupSecretValidator(options);
        var report    = await validator.ValidateAsync(cfg, isLocal, strict);

        foreach (var error in report.Errors)
            Log.Error("FAIL: {Error}", error);

        foreach (var warning in report.Warnings)
            Log.Warning("WARN: {Warning}", warning);

        if (report.Passed)
        {
            Log.Information("Validation passed — {ErrorCount} error(s), {WarnCount} warning(s).",
                report.Errors.Count, report.Warnings.Count);
            return 0;
        }

        Log.Error("Validation FAILED with {Count} error(s). Fix issues in docker/.env before running services.", report.Errors.Count);
        return 1;
    }
}
