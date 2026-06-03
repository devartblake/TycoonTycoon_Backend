using Microsoft.Extensions.Configuration;
using Serilog;
using Synaptix.Setup.Bootstrap;
using Synaptix.Setup.Services;

namespace Synaptix.Setup.Commands;

public static class ProvisionMinioCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        Log.Information("=== Synaptix.Setup provision-minio ===");
        var task   = new MinioSetupTask();
        var result = await task.RunAsync(cfg);
        if (!result.Success) { Log.Error("MinIO provision failed: {Error}", result.Error); return 1; }
        Log.Information("{Message}", result.Message);
        return 0;
    }
}

public static class UploadSeedsCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        Log.Information("=== Synaptix.Setup upload-seeds ===");
        var seedsPath = args.SkipWhile(a => a != "--source").Skip(1).FirstOrDefault()
                     ?? Path.Combine(FindRepoRoot(), "Synaptix.MigrationService", "seeds");

        var task   = new MinioSetupTask();
        var result = await task.UploadSeedsAsync(cfg, seedsPath);
        if (!result.Success) { Log.Error("Upload failed: {Error}", result.Error); return 1; }
        Log.Information("{Message}", result.Message);
        return 0;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "TycoonTycoon_Backend.slnx"))) return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }
}

public static class ValidateSeedsCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        Log.Information("=== Synaptix.Setup validate-seeds ===");
        var task   = new MinioSetupTask();
        var result = await task.ValidateSeedsAsync(cfg);
        foreach (var w in result.Warnings) Log.Warning("{Warning}", w);
        Log.Information("{Message}", result.Message ?? result.Error);
        return result.Success ? 0 : 1;
    }
}

public static class CreateSuperAdminCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        Log.Information("=== Synaptix.Setup create-super-admin ===");
        var task   = new SuperAdminSetupTask();
        var result = await task.RunAsync(cfg);
        if (!result.Success) { Log.Error("Super admin creation failed: {Error}", result.Error); return 1; }
        Log.Information("{Message}", result.Message);
        return 0;
    }
}

public static class RotateSuperAdminPasswordCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        Log.Information("=== Synaptix.Setup rotate-super-admin-password ===");

        var newPassword = args.SkipWhile(a => a != "--password").Skip(1).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            Log.Information("Enter new super admin password (or pass --password <value>):");
            newPassword = Console.ReadLine();
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            Log.Error("No password provided.");
            return 1;
        }

        var task   = new SuperAdminSetupTask();
        var result = await task.RotatePasswordAsync(cfg, newPassword);
        if (!result.Success) { Log.Error("Password rotation failed: {Error}", result.Error); return 1; }
        Log.Information("{Message}", result.Message);
        return 0;
    }
}

public static class StatusCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        Log.Information("=== Synaptix.Setup status ===");

        var repoRoot   = FindRepoRoot();
        var statusPath = Path.Combine(repoRoot, ".local", "bootstrap", "bootstrap-status.json");
        var stateReader = new BootstrapStateWriter();
        var status      = await stateReader.ReadAsync(statusPath);

        if (status is null)
        {
            Log.Warning("No bootstrap status found at {Path}.", statusPath);
            Log.Information("Run 'dotnet run --project Synaptix.Setup -- init-local' to start setup.");
            return 0;
        }

        Console.WriteLine($"""

            Bootstrap Status Report
            =======================
            Environment:        {status.Environment}
            Generated at:       {status.GeneratedAtUtc:O}
            Secrets generated:  {Bool(status.SecretsGenerated)}
            Services provisioned: {Bool(status.ServicesProvisioned)}
            Seeds uploaded:     {Bool(status.SeedsUploaded)}
            Super admin:        {Bool(status.SuperAdminCreated)}
            .env file:          {status.EnvFilePath ?? "not set"}

            Completed tasks ({status.CompletedTasks.Count}):
            {string.Join("\n  ", status.CompletedTasks.Select(t => $"  ✓ {t}"))}

            Warnings ({status.Warnings.Count}):
            {string.Join("\n", status.Warnings.Select(w => $"  ⚠ {w}"))}

            Errors ({status.Errors.Count}):
            {string.Join("\n", status.Errors.Select(e => $"  ✗ {e}"))}
            """);

        return status.Errors.Count > 0 ? 2 : 0;
    }

    private static string Bool(bool v) => v ? "✓ yes" : "✗ no";

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "TycoonTycoon_Backend.slnx"))) return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }
}
