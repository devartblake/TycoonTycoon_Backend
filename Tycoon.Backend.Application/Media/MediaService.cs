using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Media
{
    public sealed class MediaService(IObjectStorage storage)
    {
        private static readonly TimeSpan UploadExpiry = TimeSpan.FromMinutes(10);

        public async Task<UploadIntentDto> CreateUploadIntentAsync(CreateUploadIntentRequest req, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var assetKey = $"uploads/{now:yyyyMMdd}/{Guid.NewGuid():N}_{Sanitize(req.FileName)}";

            string uploadUrl;
            if (storage is IPresignedStorage presigned)
            {
                // MinIO (or any S3-compatible backend): browser uploads directly,
                // bypassing the API. The URL expires in UploadExpiry.
                uploadUrl = await presigned.GetPresignedPutUrlAsync(assetKey, req.ContentType, UploadExpiry, ct);
            }
            else
            {
                // Local dev / tests: fall back to the API-proxied upload endpoint.
                uploadUrl = $"/admin/media/upload/{Uri.EscapeDataString(assetKey)}";
            }

            return new UploadIntentDto(assetKey, uploadUrl, now.Add(UploadExpiry));
        }

        private static string Sanitize(string name)
        {
            var safe = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'));
            return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
        }
    }
}
