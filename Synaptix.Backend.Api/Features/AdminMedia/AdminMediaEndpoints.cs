using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Minio.DataModel.Args;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Media;
using Synaptix.Backend.Infrastructure.Storage;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminMedia
{
    public static class AdminMediaEndpoints
    {
        private static readonly HttpClient HealthHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/media").WithTags("Admin/Media");

            g.MapPost("/intent", async ([FromBody] CreateUploadIntentRequest req, MediaService media, CancellationToken ct) =>
            {
                try
                {
                    var dto = await media.CreateUploadIntentAsync(req, ct);
                    return Results.Ok(dto);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
                }
            });

            g.MapPost("/upload/{*assetKey}", async ([FromRoute] string assetKey, IFormFile file, IObjectStorage storage, CancellationToken ct) =>
            {
                assetKey = Uri.UnescapeDataString(assetKey);
                MediaUploadPolicy.Validate(file.FileName, file.ContentType, file.Length);
                await using var stream = file.OpenReadStream();
                await storage.PutAsync(assetKey, stream, file.ContentType, file.Length, ct);
                return Results.Ok(new { assetKey, url = storage.GetPublicUrl(assetKey) });
            }).DisableAntiforgery();

            g.MapGet("/storage-diagnostics", async (IServiceProvider services, CancellationToken ct) =>
            {
                var options = services.GetService<MinioOptions>();
                var client = services.GetService<IMinioClient>();
                var now = DateTimeOffset.UtcNow;

                if (options is null || client is null)
                {
                    return Results.Ok(new
                    {
                        overallStatus = "degraded",
                        baseUrl = (string?)null,
                        publicEndpoint = (string?)null,
                        region = "us-east-1",
                        tlsEnabled = false,
                        auth = "local-storage",
                        bucket = (string?)null,
                        bucketExists = false,
                        objectCount = 0,
                        totalBytes = 0L,
                        lastRunAtUtc = now,
                        checks = new Dictionary<string, object>
                        {
                            ["storage"] = new
                            {
                                status = "degraded",
                                httpStatus = (int?)null,
                                latencyMs = 0d,
                                endpoint = "local-storage",
                                message = "MinIO is not configured; using local object storage."
                            }
                        }
                    });
                }

                var scheme = options.UseSSL ? "https" : "http";
                var baseUrl = $"{scheme}://{options.Endpoint}".TrimEnd('/');
                var checks = new Dictionary<string, object>
                {
                    ["live"] = await ProbeAsync(baseUrl, "/minio/health/live", ct),
                    ["ready"] = await ProbeAsync(baseUrl, "/minio/health/ready", ct)
                };

                var bucketExists = false;
                var objectCount = 0;
                var totalBytes = 0L;
                var bucketStatus = "healthy";
                string? bucketMessage = null;
                var started = TimeProvider.System.GetTimestamp();

                try
                {
                    bucketExists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(options.Bucket), ct);
                    if (bucketExists)
                    {
                        await foreach (var item in client.ListObjectsEnumAsync(
                                           new ListObjectsArgs()
                                               .WithBucket(options.Bucket)
                                               .WithRecursive(true),
                                           ct))
                        {
                            objectCount++;
                            totalBytes += checked((long)item.Size);
                        }
                    }
                    else
                    {
                        bucketStatus = "degraded";
                        bucketMessage = $"Bucket '{options.Bucket}' does not exist.";
                    }
                }
                catch (Exception ex)
                {
                    bucketStatus = "degraded";
                    bucketMessage = ex.Message;
                }

                checks["bucket"] = new
                {
                    status = bucketStatus,
                    httpStatus = (int?)null,
                    latencyMs = Math.Round(TimeProvider.System.GetElapsedTime(started).TotalMilliseconds, 2),
                    endpoint = $"s3://{options.Bucket}",
                    message = bucketMessage ?? "Bucket probe completed."
                };

                var statuses = checks.Values
                    .Select(x => x.GetType().GetProperty("status")?.GetValue(x)?.ToString() ?? "degraded")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var overallStatus = statuses.Contains("offline")
                    ? "offline"
                    : statuses.Contains("degraded") ? "degraded" : "healthy";

                return Results.Ok(new
                {
                    overallStatus,
                    baseUrl,
                    publicEndpoint = options.PublicEndpoint,
                    region = "us-east-1",
                    tlsEnabled = options.UseSSL,
                    auth = "access-key",
                    bucket = options.Bucket,
                    bucketExists,
                    objectCount,
                    totalBytes,
                    lastRunAtUtc = now,
                    checks
                });
            });
        }

        private static async Task<object> ProbeAsync(string baseUrl, string path, CancellationToken ct)
        {
            var started = TimeProvider.System.GetTimestamp();
            var url = $"{baseUrl}{path}";
            try
            {
                using var response = await HealthHttpClient.GetAsync(url, ct);
                return new
                {
                    status = response.IsSuccessStatusCode ? "healthy" : "degraded",
                    httpStatus = (int)response.StatusCode,
                    latencyMs = Math.Round(TimeProvider.System.GetElapsedTime(started).TotalMilliseconds, 2),
                    endpoint = path,
                    message = (await response.Content.ReadAsStringAsync(ct)).Trim()
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return new
                {
                    status = "offline",
                    httpStatus = (int?)null,
                    latencyMs = Math.Round(TimeProvider.System.GetElapsedTime(started).TotalMilliseconds, 2),
                    endpoint = path,
                    message = ex.Message
                };
            }
        }
    }
}
