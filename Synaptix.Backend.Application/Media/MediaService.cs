using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Media
{
    public sealed class MediaService(IObjectStorage storage)
    {
        private static readonly TimeSpan UploadExpiry = TimeSpan.FromMinutes(10);

        public async Task<UploadIntentDto> CreateUploadIntentAsync(CreateUploadIntentRequest req, CancellationToken ct = default)
        {
            var policy = MediaUploadPolicy.Validate(req.FileName, req.ContentType, req.SizeBytes);
            var now = DateTimeOffset.UtcNow;
            var assetKey = $"uploads/{policy.Category}/{now:yyyyMMdd}/{Guid.NewGuid():N}_{Sanitize(req.FileName)}";
            return await CreateUploadIntentForAssetKeyAsync(assetKey, req.ContentType, ct);
        }

        public async Task<UploadIntentDto> CreateUploadIntentForAssetKeyAsync(string assetKey, string contentType, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;

            string uploadUrl;
            if (storage is IPresignedStorage presigned)
            {
                // MinIO (or any S3-compatible backend): browser uploads directly,
                // bypassing the API. The URL expires in UploadExpiry.
                uploadUrl = await presigned.GetPresignedPutUrlAsync(assetKey, contentType, UploadExpiry, ct);
            }
            else
            {
                // Local dev / tests: fall back to the API-proxied upload endpoint.
                uploadUrl = $"/admin/media/upload/{Uri.EscapeDataString(assetKey)}";
            }

            return new UploadIntentDto(assetKey, uploadUrl, now.Add(UploadExpiry));
        }

        public string GetPublicUrl(string assetKey) => storage.GetPublicUrl(assetKey);

        private static string Sanitize(string name)
        {
            var safe = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'));
            return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
        }
    }

    public sealed record MediaUploadPolicyResult(string Category, long MaxBytes);

    public static class MediaUploadPolicy
    {
        private const long MiB = 1024 * 1024;

        private static readonly Dictionary<string, (string Category, long MaxBytes, string[] ContentTypes)> Rules = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ("images", 25 * MiB, ["image/jpeg"]),
            [".jpeg"] = ("images", 25 * MiB, ["image/jpeg"]),
            [".png"] = ("images", 25 * MiB, ["image/png"]),
            [".webp"] = ("images", 25 * MiB, ["image/webp"]),
            [".gif"] = ("images", 25 * MiB, ["image/gif"]),
            [".avif"] = ("images", 25 * MiB, ["image/avif"]),
            [".glb"] = ("3d", 100 * MiB, ["model/gltf-binary", "application/octet-stream"]),
            [".gltf"] = ("3d", 100 * MiB, ["model/gltf+json"]),
            [".mp3"] = ("audio", 100 * MiB, ["audio/mpeg"]),
            [".wav"] = ("audio", 100 * MiB, ["audio/wav", "audio/x-wav"]),
            [".ogg"] = ("audio", 100 * MiB, ["audio/ogg"]),
            [".aac"] = ("audio", 100 * MiB, ["audio/aac"]),
            [".m4a"] = ("audio", 100 * MiB, ["audio/mp4"]),
            [".mp4"] = ("video", 500 * MiB, ["video/mp4"]),
            [".webm"] = ("video", 500 * MiB, ["video/webm", "audio/webm"]),
            [".mov"] = ("video", 500 * MiB, ["video/quicktime"]),
        };

        public static MediaUploadPolicyResult Validate(string fileName, string contentType, long sizeBytes)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException("fileName is required.");
            if (string.IsNullOrWhiteSpace(contentType))
                throw new InvalidOperationException("contentType is required.");
            if (sizeBytes <= 0)
                throw new InvalidOperationException("sizeBytes must be greater than zero.");

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!Rules.TryGetValue(extension, out var rule))
                throw new InvalidOperationException($"Unsupported media file extension '{extension}'.");

            var normalizedContentType = contentType.Split(';', 2)[0].Trim();
            if (!rule.ContentTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unsupported content type '{contentType}' for '{extension}'.");

            if (sizeBytes > rule.MaxBytes)
                throw new InvalidOperationException($"File exceeds the {rule.MaxBytes} byte limit for {rule.Category} uploads.");

            return new MediaUploadPolicyResult(rule.Category, rule.MaxBytes);
        }
    }
}
