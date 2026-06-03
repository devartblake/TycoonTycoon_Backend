using Microsoft.Extensions.Configuration;
using Serilog;
using Synaptix.Setup.Bootstrap;
using Synaptix.Setup.Services;

namespace Synaptix.Setup.Commands;

public static class ProvisionServicesCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        var strict     = args.Contains("--strict");
        var statusPath = Path.Combine(FindRepoRoot(), ".local", "bootstrap", "bootstrap-status.json");
        var stateWriter = new BootstrapStateWriter();

        Log.Information("=== Synaptix.Setup provision-services (strict={Strict}) ===", strict);

        ISetupTask[] tasks =
        [
            new PostgresSetupTask(),
            new MongoSetupTask(),
            new RedisSetupTask(),
            new RabbitMqSetupTask(),
            new MinioSetupTask(),
            new ElasticsearchSetupTask(),
        ];

        var completed  = new List<string>();
        var warnings   = new List<string>();
        var errors     = new List<string>();
        var allPassed  = true;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            new CancellationToken()); // allows future timeout support

        foreach (var task in tasks)
        {
            Log.Information("→ Provisioning {Task}…", task.Name);
            try
            {
                var result = await task.RunAsync(cfg, cts.Token);
                if (result.Success)
                {
                    Log.Information("  ✓ {Task}: {Message}", task.Name, result.Message);
                    completed.Add(task.Name);
                    warnings.AddRange(result.Warnings.Select(w => $"{task.Name}: {w}"));
                }
                else
                {
                    Log.Error("  ✗ {Task}: {Error}", task.Name, result.Error);
                    errors.Add($"{task.Name}: {result.Error}");
                    allPassed = false;
                    if (strict) break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "  ✗ {Task}: unhandled exception.", task.Name);
                errors.Add($"{task.Name}: {ex.Message}");
                allPassed = false;
                if (strict) break;
            }
        }

        // Also upload seeds
        Log.Information("→ Uploading bundled seed files to MinIO…");
        var minioTask   = new MinioSetupTask();
        var seedsSource = cfg["Setup:SeedsSourcePath"] ?? Path.Combine(FindRepoRoot(), "Synaptix.MigrationService", "seeds");
        var seedResult  = await minioTask.UploadSeedsAsync(cfg, seedsSource, cts.Token);
        if (seedResult.Success)
        {
            Log.Information("  ✓ Seeds: {Message}", seedResult.Message);
            completed.Add("SeedsUpload");
        }
        else
        {
            Log.Warning("  ⚠ Seeds: {Message}", seedResult.Message ?? seedResult.Error);
            warnings.Add($"Seeds: {seedResult.Message ?? seedResult.Error}");
        }

        var status = await stateWriter.ReadAsync(statusPath) ?? new BootstrapStatus();
        status.ServicesProvisioned = allPassed;
        status.SeedsUploaded       = seedResult.Success;
        status.CompletedTasks.AddRange(completed);
        status.Warnings.AddRange(warnings);
        status.Errors.AddRange(errors);
        await stateWriter.WriteAsync(statusPath, status);

        if (!allPassed && strict)
        {
            Log.Error("Provisioning FAILED. Fix the above errors and retry.");
            return 1;
        }

        Log.Information("Provision-services complete — {Count} task(s) succeeded, {Errors} error(s), {Warns} warning(s).",
            completed.Count, errors.Count, warnings.Count);
        return allPassed ? 0 : 2; // 2 = partial success
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "TycoonTycoon_Backend.slnx")) ||
                File.Exists(Path.Combine(dir, "TycoonTycoon_Backend.sln")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }
}
