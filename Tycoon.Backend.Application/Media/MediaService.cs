using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Media
{
    public sealed class MediaService
    {
        // For now: local uploads. Later: return S3/Cloudflare R2 presigned URLs.
        public UploadIntentDto CreateUploadIntent(CreateUploadIntentRequest req)
        {
            var now = DateTimeOffset.UtcNow;
            var assetKey = $"uploads/{now:yyyyMMdd}/{Guid.NewGuid():N}_{Sanitize(req.FileName)}";
            var uploadUrl = $"/admin/media/upload/{Uri.EscapeDataString(assetKey)}";
            return new UploadIntentDto(assetKey, uploadUrl, now.AddMinutes(10));
        }

        private static string Sanitize(string name)
        {
            var safe = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'));
            return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
        }
    }
}
