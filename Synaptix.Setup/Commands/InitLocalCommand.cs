using Microsoft.Extensions.Configuration;
using Serilog;
using Synaptix.Setup.Bootstrap;
using Synaptix.Setup.Secrets;
using Synaptix.Setup.Security;

namespace Synaptix.Setup.Commands;

public static class InitLocalCommand
{
    public static async Task<int> RunAsync(IConfiguration cfg, string[] args)
    {
        var repoRoot     = FindRepoRoot();
        var templatePath = Path.Combine(repoRoot, "docker", ".env.example");
        var outputPath   = Path.Combine(repoRoot, "docker", ".env");
        var statusPath   = Path.Combine(repoRoot, ".local", "bootstrap", "bootstrap-status.json");
        var superAdminPath = Path.Combine(repoRoot, ".local", "bootstrap", "super-admin.local.txt");

        Log.Information("=== Synaptix.Setup init-local ===");

        if (!File.Exists(templatePath))
        {
            Log.Error("Template not found: {Path}", templatePath);
            return 1;
        }

        if (File.Exists(outputPath) && !args.Contains("--force"))
        {
            Log.Warning("docker/.env already exists. Use --force to overwrite.");
            Log.Information("Run 'dotnet run --project Synaptix.Setup -- status' to check current state.");
            return 0;
        }

        var writer = new SecretManifestWriter();
        var result = await writer.WriteEnvAsync(templatePath, outputPath);

        Log.Information("Generated docker/.env with {Count} secret(s).", result.Values.Count);

        // Write super-admin credential file for local dev convenience
        if (result.Values.TryGetValue("SUPER_ADMIN_PASSWORD", out var adminPass))
        {
            var email = cfg["SUPER_ADMIN_EMAIL"] ?? "admin@tycoon.local";
            Directory.CreateDirectory(Path.GetDirectoryName(superAdminPath)!);
            await File.WriteAllTextAsync(superAdminPath, $"""
                Synaptix Super Admin — LOCAL ONLY
                ===================================
                Email:        {email}
                Password:     {adminPass}
                GeneratedAt:  {DateTimeOffset.UtcNow:O}

                This file is gitignored. Keep it secret.
                """);
            Log.Information("Super admin credentials written to .local/bootstrap/super-admin.local.txt");
        }

        // Write protected secret manifest (Phase 1: plaintext-local)
        var protector     = new PlaintextLocalSetupSecretProtector();
        var manifestPath  = Path.Combine(repoRoot, ".local", "bootstrap", "bootstrap.secrets.enc.json");
        var manifest = new SetupSecretManifest { Environment = "local" };
        foreach (var (k, v) in result.Values)
            manifest.Secrets[k] = await protector.ProtectAsync(k, v);
        await manifest.SaveAsync(manifestPath);
        Log.Information("Secret manifest written to .local/bootstrap/bootstrap.secrets.enc.json (ProtectionMode=PlaintextLocal).");

        // Write bootstrap status
        var status = new BootstrapStatus
        {
            Environment      = "local",
            SecretsGenerated = true,
            EnvFilePath      = outputPath,
            CompletedTasks   = ["init-local"],
        };
        var stateWriter = new BootstrapStateWriter();
        await stateWriter.WriteAsync(statusPath, status);

        Log.Information("");
        Log.Information("Next steps:");
        Log.Information("  1. docker compose -f docker/compose.yml up -d postgres mongodb redis rabbitmq minio");
        Log.Information("  2. dotnet run --project Synaptix.Setup -- provision-services");
        Log.Information("  3. dotnet run --project Synaptix.MigrationService");
        Log.Information("  4. docker compose -f docker/compose.yml up -d");

        return 0;
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
