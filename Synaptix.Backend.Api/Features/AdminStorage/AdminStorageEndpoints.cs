using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Minio.DataModel.Args;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Infrastructure.Storage;

namespace Synaptix.Backend.Api.Features.AdminStorage;

public static class AdminStorageEndpoints
{
    private static readonly TimeSpan UploadExpiry = TimeSpan.FromMinutes(10);
    private const long MiB = 1024 * 1024;

    private static readonly StoragePrefixPolicy[] Prefixes =
    [
        new("seeds/", "Seed data", "Canonical JSON seed inputs for the migration service.", 50 * MiB),
        new("avatars/", "Avatars", "Avatar media and player-facing avatar assets.", 100 * MiB),
        new("avatar-packages/", "Avatar packages", "Zip archives and bundled avatar packages.", 250 * MiB),
        new("songs/", "Songs", "Music and long-form audio assets.", 150 * MiB),
        new("audio/", "Audio", "SFX and short audio assets.", 100 * MiB),
        new("models/", "3D models", "GLB, GLTF, FBX, OBJ, and related 3D files.", 250 * MiB),
        new("images/", "Images", "Images, icons, screenshots, and previews.", 50 * MiB),
        new("videos/", "Videos", "Video assets.", 500 * MiB),
        new("frontend/assets/", "Frontend assets", "Static frontend config, shaders, data, and support assets.", 100 * MiB),
        new("questions/", "Question datasets", "Question datasets and question support files.", 100 * MiB),
    ];

    private static readonly HashSet<string> ProtectedSeedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "seeds/store-items.json",
        "seeds/skill-nodes.json",
        "seeds/season-rewards.json",
        "seeds/questions.json",
    };

    private static readonly Dictionary<string, StorageFilePolicy> FilePolicies = new(StringComparer.OrdinalIgnoreCase)
    {
        [".json"] = new("application/json", 50 * MiB),
        [".jsonl"] = new("application/x-ndjson", 100 * MiB),
        [".glb"] = new("model/gltf-binary", 250 * MiB),
        [".gltf"] = new("model/gltf+json", 100 * MiB),
        [".fbx"] = new("application/octet-stream", 250 * MiB),
        [".obj"] = new("text/plain", 100 * MiB),
        [".mtl"] = new("text/plain", 25 * MiB),
        [".zip"] = new("application/zip", 250 * MiB),
        [".mp3"] = new("audio/mpeg", 150 * MiB),
        [".wav"] = new("audio/wav", 150 * MiB),
        [".ogg"] = new("audio/ogg", 100 * MiB),
        [".m4a"] = new("audio/mp4", 100 * MiB),
        [".aac"] = new("audio/aac", 100 * MiB),
        [".png"] = new("image/png", 50 * MiB),
        [".jpg"] = new("image/jpeg", 50 * MiB),
        [".jpeg"] = new("image/jpeg", 50 * MiB),
        [".webp"] = new("image/webp", 50 * MiB),
        [".gif"] = new("image/gif", 50 * MiB),
        [".svg"] = new("image/svg+xml", 10 * MiB),
        [".mp4"] = new("video/mp4", 500 * MiB),
        [".webm"] = new("video/webm", 500 * MiB),
        [".mov"] = new("video/quicktime", 500 * MiB),
        [".css"] = new("text/css", 10 * MiB),
        [".js"] = new("text/javascript", 10 * MiB),
        [".html"] = new("text/html", 10 * MiB),
        [".txt"] = new("text/plain", 10 * MiB),
        [".frag"] = new("text/plain", 10 * MiB),
    };

    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/storage").WithTags("Admin/Storage");

        g.MapGet("/prefixes", (HttpContext http) =>
        {
            if (!HasScope(http, "storage:read"))
                return Results.Forbid();

            return Results.Ok(new
            {
                prefixes = Prefixes,
                extensions = FilePolicies.Select(x => new
                {
                    extension = x.Key,
                    contentType = x.Value.ContentType,
                    maxBytes = x.Value.MaxBytes,
                }),
                protectedKeys = ProtectedSeedKeys.OrderBy(x => x),
            });
        });

        g.MapGet("/objects", async (
            HttpContext http,
            [FromServices] IServiceProvider services,
            [FromServices] IAppDb db,
            [FromQuery] string? prefix,
            [FromQuery] string? cursor,
            [FromQuery] int? pageSize,
            CancellationToken ct) =>
        {
            if (!HasScope(http, "storage:read"))
                return Results.Forbid();

            var normalizedPrefix = NormalizePrefix(prefix);
            if (normalizedPrefix is null)
                return await RejectAsync(db, http, "storage.object_list_rejected", "invalid_prefix", new { prefix }, ct);

            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            var objects = await ListObjectsAsync(services, normalizedPrefix, cursor, size, ct);
            await AuditAsync(db, http, "storage.object_list", "success", new { prefix = normalizedPrefix, pageSize = size, count = objects.Items.Count }, ct);
            return Results.Ok(objects);
        });

        g.MapGet("/objects/metadata", async (
            HttpContext http,
            [FromServices] IServiceProvider services,
            [FromServices] IAppDb db,
            [FromQuery] string key,
            CancellationToken ct) =>
        {
            if (!HasScope(http, "storage:read"))
                return Results.Forbid();

            var validation = ValidateObjectPath(key);
            if (!validation.Ok)
                return await RejectAsync(db, http, "storage.metadata_rejected", validation.Error!, new { key }, ct);

            var metadata = await GetMetadataAsync(services, validation.Key!, ct);
            if (metadata is null)
                return Results.NotFound(new { code = "NOT_FOUND", message = "Object metadata was not found." });

            await AuditAsync(db, http, "storage.metadata", "success", new { key = validation.Key }, ct);
            return Results.Ok(metadata);
        });

        g.MapPost("/upload-intent", async (
            HttpContext http,
            [FromBody] StorageUploadIntentRequest req,
            [FromServices] IObjectStorage storage,
            [FromServices] IServiceProvider services,
            [FromServices] IAppDb db,
            CancellationToken ct) =>
        {
            if (!HasScope(http, "storage:write"))
                return Results.Forbid();

            var validation = ValidateKey(req.Key, req.ContentType, req.SizeBytes, req.Overwrite);
            if (!validation.Ok)
                return await RejectAsync(db, http, "storage.upload_intent_rejected", validation.Error!, new { req.Key, req.ContentType, req.SizeBytes }, ct);

            var exists = await ObjectExistsAsync(services, storage, validation.Key!, ct);
            if (exists && !req.Overwrite)
                return await RejectAsync(db, http, "storage.upload_intent_rejected", "overwrite_required", new { key = validation.Key }, ct);

            if (storage is not IPresignedStorage presigned)
                return Results.BadRequest(new { code = "PRESIGN_UNAVAILABLE", message = "Storage backend does not support presigned uploads; use upload-proxy." });

            var uploadUrl = await presigned.GetPresignedPutUrlAsync(validation.Key!, validation.ContentType!, UploadExpiry, ct);
            await AuditAsync(db, http, "storage.upload_intent", "success", new { key = validation.Key, req.SizeBytes, overwrite = req.Overwrite }, ct);
            return Results.Ok(new
            {
                key = validation.Key,
                uploadUrl,
                expiresAtUtc = DateTimeOffset.UtcNow.Add(UploadExpiry),
                contentType = validation.ContentType,
                overwrite = req.Overwrite,
            });
        });

        g.MapPost("/upload-proxy", async (
            HttpContext http,
            [FromForm] string key,
            [FromForm] bool overwrite,
            IFormFile file,
            [FromServices] IObjectStorage storage,
            [FromServices] IServiceProvider services,
            [FromServices] IAppDb db,
            CancellationToken ct) =>
        {
            if (!HasScope(http, "storage:write"))
                return Results.Forbid();

            var validation = ValidateKey(key, file.ContentType, file.Length, overwrite);
            if (!validation.Ok)
                return await RejectAsync(db, http, "storage.upload_rejected", validation.Error!, new { key, file.FileName, file.ContentType, file.Length }, ct);

            var exists = await ObjectExistsAsync(services, storage, validation.Key!, ct);
            if (exists && !overwrite)
                return await RejectAsync(db, http, "storage.upload_rejected", "overwrite_required", new { key = validation.Key }, ct);

            await using var stream = file.OpenReadStream();
            await storage.PutAsync(validation.Key!, stream, validation.ContentType!, file.Length, ct);
            await AuditAsync(db, http, "storage.upload", "success", new { key = validation.Key, file.FileName, file.Length, overwrite }, ct);
            return Results.Ok(new { key = validation.Key, url = storage.GetPublicUrl(validation.Key!), overwritten = exists });
        }).DisableAntiforgery();
    }

    private static async Task<IResult> RejectAsync(IAppDb db, HttpContext http, string title, string reason, object metadata, CancellationToken ct)
    {
        await AuditAsync(db, http, title, "rejected", new { reason, metadata }, ct);
        return Results.BadRequest(new { code = reason.ToUpperInvariant(), message = reason.Replace('_', ' ') });
    }

    private static async Task AuditAsync(IAppDb db, HttpContext http, string title, string status, object metadata, CancellationToken ct)
    {
        var email = http.User.FindFirst(ClaimTypes.Email)?.Value
                    ?? http.Request.Headers["X-Admin-User"].FirstOrDefault()
                    ?? "unknown";
        await AdminSecurityAudit.WriteAsync(db, title, status, new
        {
            actor = email,
            ip = http.Connection.RemoteIpAddress?.ToString(),
            metadata,
        }, ct);
    }

    private static bool HasScope(HttpContext http, string requiredScope)
    {
        if (http.RequestServices.GetRequiredService<IConfiguration>().GetValue("Testing:UseInMemoryDb", false)
            && http.User?.Identity?.IsAuthenticated != true)
            return true;

        var scope = http.User.FindFirst("scope")?.Value;
        return scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(requiredScope, StringComparer.OrdinalIgnoreCase) == true;
    }

    private static string? NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return "";

        // Prefixes are canonically written with a trailing slash (e.g. "seeds/"),
        // but NormalizeKey validates *file keys* and rejects a trailing slash
        // (the empty final segment). Strip trailing slashes before validation and
        // re-add a single one below, so the canonical prefix form is accepted.
        var cleaned = NormalizeKey(prefix.TrimEnd('/'));
        if (cleaned is null)
            return null;

        if (cleaned.Length > 0 && !cleaned.EndsWith('/'))
            cleaned += "/";

        return cleaned.Length == 0 || Prefixes.Any(p => cleaned.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase))
            ? cleaned
            : null;
    }

    private static StorageValidation ValidateKey(string key, string contentType, long sizeBytes, bool overwrite)
    {
        var cleaned = NormalizeKey(key);
        if (string.IsNullOrWhiteSpace(cleaned))
            return StorageValidation.Fail("key_required");
        if (cleaned.EndsWith('/'))
            return StorageValidation.Fail("key_must_reference_file");
        if (!Prefixes.Any(p => cleaned.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase)))
            return StorageValidation.Fail("prefix_not_allowed");
        if (ProtectedSeedKeys.Contains(cleaned) && !overwrite)
            return StorageValidation.Fail("protected_seed_overwrite_requires_confirmation");

        var extension = Path.GetExtension(cleaned).ToLowerInvariant();
        if (!FilePolicies.TryGetValue(extension, out var policy))
            return StorageValidation.Fail("extension_not_allowed");
        if (sizeBytes <= 0)
            return StorageValidation.Fail("size_required");
        if (sizeBytes > policy.MaxBytes || sizeBytes > Prefixes.First(p => cleaned.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase)).MaxBytes)
            return StorageValidation.Fail("file_too_large");

        var normalizedContentType = NormalizeContentType(contentType);
        if (!IsContentTypeAllowed(extension, normalizedContentType, policy.ContentType))
            return StorageValidation.Fail("content_type_not_allowed");

        return StorageValidation.Success(cleaned, normalizedContentType);
    }

    private static StorageValidation ValidateObjectPath(string key)
    {
        var cleaned = NormalizeKey(key);
        if (string.IsNullOrWhiteSpace(cleaned))
            return StorageValidation.Fail("key_required");
        if (cleaned.EndsWith('/'))
            return StorageValidation.Fail("key_must_reference_file");
        if (!Prefixes.Any(p => cleaned.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase)))
            return StorageValidation.Fail("prefix_not_allowed");
        return StorageValidation.Success(cleaned, null);
    }

    private static string? NormalizeKey(string value)
    {
        var cleaned = value.Replace('\\', '/').Trim();
        if (cleaned.StartsWith('/'))
            return null;
        if (cleaned.Contains("//", StringComparison.Ordinal) || cleaned.Split('/').Any(segment => segment is "." or ".." or ""))
            return null;
        return cleaned.Any(char.IsControl) || Regex.IsMatch(cleaned, @"\s+$") ? null : cleaned;
    }

    private static string NormalizeContentType(string value) =>
        string.IsNullOrWhiteSpace(value) ? "application/octet-stream" : value.Split(';', 2)[0].Trim();

    private static bool IsContentTypeAllowed(string extension, string actual, string expected) =>
        actual.Equals(expected, StringComparison.OrdinalIgnoreCase)
        || (extension is ".fbx" && actual.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        || (extension is ".obj" or ".mtl" or ".frag" && actual.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
        || (extension is ".jsonl" && actual.Equals("application/json", StringComparison.OrdinalIgnoreCase))
        || actual.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);

    private static async Task<StorageObjectListResponse> ListObjectsAsync(IServiceProvider services, string prefix, string? cursor, int pageSize, CancellationToken ct)
    {
        var options = services.GetService<MinioOptions>();
        var client = services.GetService<IMinioClient>();
        if (options is null || client is null)
            return new StorageObjectListResponse("local-storage", prefix, null, []);

        var items = new List<StorageObjectSummary>();
        var skip = ParseCursor(cursor);
        var index = 0;
        await foreach (var item in client.ListObjectsEnumAsync(new ListObjectsArgs().WithBucket(options.Bucket).WithPrefix(prefix).WithRecursive(true), ct))
        {
            var key = GetString(item, "Key") ?? "";
            if (!Prefixes.Any(p => key.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (index++ < skip)
                continue;
            if (items.Count >= pageSize)
                break;

            items.Add(new StorageObjectSummary(
                key,
                GetLong(item, "Size"),
                null,
                GetString(item, "ETag"),
                GetDateTimeOffset(item, "LastModifiedDateTime") ?? GetDateTimeOffset(item, "LastModified"),
                CategoryFor(key)));
        }

        var nextCursor = items.Count == pageSize ? (skip + items.Count).ToString() : null;
        return new StorageObjectListResponse(options.Bucket, prefix, nextCursor, items);
    }

    private static async Task<StorageObjectMetadata?> GetMetadataAsync(IServiceProvider services, string key, CancellationToken ct)
    {
        var options = services.GetService<MinioOptions>();
        var client = services.GetService<IMinioClient>();
        if (options is null || client is null)
            return null;

        try
        {
            var stat = await client.StatObjectAsync(new StatObjectArgs().WithBucket(options.Bucket).WithObject(key), ct);
            return new StorageObjectMetadata(
                key,
                GetLong(stat, "Size"),
                GetString(stat, "ContentType"),
                GetString(stat, "ETag"),
                GetDateTimeOffset(stat, "LastModified"),
                CategoryFor(key),
                GetDictionary(stat, "MetaData"));
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> ObjectExistsAsync(IServiceProvider services, IObjectStorage storage, string key, CancellationToken ct)
    {
        if (await GetMetadataAsync(services, key, ct) is not null)
            return true;

        await using var local = await storage.GetAsync(key, ct);
        return local is not null;
    }

    private static int ParseCursor(string? cursor) => int.TryParse(cursor, out var parsed) && parsed > 0 ? parsed : 0;

    private static string CategoryFor(string key) =>
        Prefixes.FirstOrDefault(p => key.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase))?.Prefix.TrimEnd('/') ?? "unknown";

    private static string? GetString(object source, string property) =>
        source.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source)?.ToString();

    private static long GetLong(object source, string property)
    {
        var value = source.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);
        return value switch
        {
            long l => l,
            ulong ul => checked((long)ul),
            int i => i,
            _ => 0,
        };
    }

    private static DateTimeOffset? GetDateTimeOffset(object source, string property)
    {
        var value = source.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt),
            _ => null,
        };
    }

    private static IReadOnlyDictionary<string, string> GetDictionary(object source, string property)
    {
        var value = source.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);
        return value as IReadOnlyDictionary<string, string> ?? new Dictionary<string, string>();
    }

    private sealed record StoragePrefixPolicy(string Prefix, string Label, string Description, long MaxBytes);
    private sealed record StorageFilePolicy(string ContentType, long MaxBytes);
    private sealed record StorageUploadIntentRequest(string Key, string ContentType, long SizeBytes, bool Overwrite = false);
    private sealed record StorageObjectListResponse(string Bucket, string Prefix, string? NextCursor, IReadOnlyList<StorageObjectSummary> Items);
    private sealed record StorageObjectSummary(string Key, long SizeBytes, string? ContentType, string? ETag, DateTimeOffset? LastModifiedUtc, string Category);
    private sealed record StorageObjectMetadata(string Key, long SizeBytes, string? ContentType, string? ETag, DateTimeOffset? LastModifiedUtc, string Category, IReadOnlyDictionary<string, string> Metadata);

    private sealed record StorageValidation(bool Ok, string? Key, string? ContentType, string? Error)
    {
        public static StorageValidation Success(string key, string? contentType) => new(true, key, contentType, null);
        public static StorageValidation Fail(string error) => new(false, null, null, error);
    }
}
