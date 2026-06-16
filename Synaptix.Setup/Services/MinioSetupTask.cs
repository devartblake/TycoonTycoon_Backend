using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Serilog;

namespace Synaptix.Setup.Services;

public sealed class MinioSetupTask : ISetupTask
{
    public string Name => "MinIO";

    public async Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var endpoint  = cfg["MinIO:Endpoint"]  ?? cfg["MINIO_ENDPOINT"] ?? "localhost:9000";
        var accessKey = cfg["MinIO:AccessKey"] ?? cfg["MINIO_ROOT_USER"];
        var secretKey = cfg["MinIO:SecretKey"] ?? cfg["MINIO_ROOT_PASSWORD"];
        var bucket    = cfg["MinIO:Bucket"]    ?? cfg["MINIO_BUCKET"] ?? "synaptix-assets";
        var useSSL    = bool.TryParse(cfg["MinIO:UseSSL"] ?? cfg["MINIO_USE_SSL"] ?? "false", out var ssl) && ssl;

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            return SetupResult.Fail("MinIO credentials (MINIO_ROOT_USER / MINIO_ROOT_PASSWORD) are not set.");

        try
        {
            var client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(useSSL)
                .Build();

            // Create bucket if it doesn't exist
            var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
            if (!exists)
            {
                await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
                Log.Information("MinIO: created bucket '{Bucket}'.", bucket);
            }
            else
            {
                Log.Information("MinIO: bucket '{Bucket}' already exists.", bucket);
            }

            return SetupResult.Ok($"MinIO provisioned — bucket: '{bucket}' on {endpoint}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MinIO setup failed.");
            return SetupResult.Fail($"MinIO setup failed: {ex.Message}");
        }
    }

    public async Task<SetupResult> UploadSeedsAsync(IConfiguration cfg, string seedsSourcePath, CancellationToken ct = default)
    {
        var endpoint  = cfg["MinIO:Endpoint"]  ?? cfg["MINIO_ENDPOINT"] ?? "localhost:9000";
        var accessKey = cfg["MinIO:AccessKey"] ?? cfg["MINIO_ROOT_USER"];
        var secretKey = cfg["MinIO:SecretKey"] ?? cfg["MINIO_ROOT_PASSWORD"];
        var bucket    = cfg["MinIO:Bucket"]    ?? cfg["MINIO_BUCKET"] ?? "synaptix-assets";
        var useSSL    = bool.TryParse(cfg["MinIO:UseSSL"] ?? "false", out var ssl) && ssl;

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            return SetupResult.Fail("MinIO credentials not set.");

        if (!Directory.Exists(seedsSourcePath))
            return SetupResult.Skip($"Seeds source path not found: {seedsSourcePath}");

        var client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSSL)
            .Build();

        var uploaded = 0;

        foreach (var file in Directory.EnumerateFiles(seedsSourcePath, "*.json", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(seedsSourcePath, file).Replace('\\', '/');
            var key = $"seeds/{relativePath}";

            await using var stream = File.OpenRead(file);
            await client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(new FileInfo(file).Length)
                .WithContentType("application/json"), ct);

            Log.Information("MinIO: uploaded seed '{Key}'.", key);
            uploaded++;
        }

        return SetupResult.Ok($"Seeds upload complete — {uploaded} uploaded.");
    }

    public async Task<SetupResult> ValidateSeedsAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var endpoint  = cfg["MinIO:Endpoint"]  ?? "localhost:9000";
        var accessKey = cfg["MinIO:AccessKey"] ?? cfg["MINIO_ROOT_USER"];
        var secretKey = cfg["MinIO:SecretKey"] ?? cfg["MINIO_ROOT_PASSWORD"];
        var bucket    = cfg["MinIO:Bucket"]    ?? "synaptix-assets";
        var useSSL    = bool.TryParse(cfg["MinIO:UseSSL"] ?? "false", out var ssl) && ssl;

        var requiredKeys = new[]
        {
            cfg["MinIO:Seeds:StoreItemsKey"]   ?? "seeds/store-items.json",
            cfg["MinIO:Seeds:SkillNodesKey"]    ?? "seeds/skill-nodes.json",
            cfg["MinIO:Seeds:SeasonRewardsKey"] ?? "seeds/season-rewards.json",
            cfg["MinIO:Seeds:QuestionsKey"]     ?? "seeds/questions.json",
            cfg["MinIO:Seeds:AssetCatalogKey"]  ?? "seeds/asset-catalog.json",
        };

        if (string.IsNullOrWhiteSpace(accessKey)) return SetupResult.Fail("MinIO credentials not set.");

        var client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey!, secretKey!)
            .WithSSL(useSSL)
            .Build();

        var missing = new List<string>();
        foreach (var key in requiredKeys)
        {
            try
            {
                await client.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(key), ct);
                Log.Debug("MinIO seed '{Key}' present.", key);
            }
            catch
            {
                missing.Add(key);
            }
        }

        if (missing.Count > 0)
        {
            Log.Warning("MinIO: missing seed files: {Missing}", string.Join(", ", missing));
            return SetupResult.Ok("Seed validation complete with warnings.", missing.Select(m => $"Missing: {m}").ToList());
        }

        return SetupResult.Ok("All required MinIO seed files are present.");
    }
}
