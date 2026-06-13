using Minio;
using Minio.DataModel.Args;
using Synaptix.Backend.Application.Abstractions;

namespace Synaptix.Backend.Infrastructure.Storage
{
    public sealed class MinioObjectStorage : IObjectStorage, IPresignedStorage
    {
        private readonly IMinioClient _client;
        private readonly MinioOptions _options;
        private bool _bucketEnsured;

        public MinioObjectStorage(IMinioClient client, MinioOptions options)
        {
            _client = client;
            _options = options;
        }

        public async Task PutAsync(string key, Stream content, string contentType, long size = -1, CancellationToken ct = default)
        {
            await EnsureBucketExistsAsync(ct);

            var args = new PutObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key)
                .WithStreamData(content)
                .WithObjectSize(size)
                .WithContentType(contentType);

            await _client.PutObjectAsync(args, ct);
        }

        public string GetPublicUrl(string key)
        {
            var host = _options.PublicEndpoint ?? _options.Endpoint;
            var scheme = _options.UseSSL ? "https" : "http";
            return $"{scheme}://{host}/{_options.Bucket}/{key}";
        }

        public async Task<string> GetPresignedPutUrlAsync(string key, string contentType, TimeSpan expiry, CancellationToken ct = default)
        {
            await EnsureBucketExistsAsync(ct);

            var args = new PresignedPutObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key)
                .WithExpiry((int)expiry.TotalSeconds);

            var internalUrl = await _client.PresignedPutObjectAsync(args);

            // Rewrite the internal host (minio:9000) to the public-facing host when
            // PublicEndpoint is configured — the browser calling the URL needs a host
            // it can reach, not the internal container hostname.
            if (!string.IsNullOrWhiteSpace(_options.PublicEndpoint) &&
                Uri.TryCreate(internalUrl, UriKind.Absolute, out var parsed))
            {
                var publicScheme = _options.UseSSL ? "https" : "http";
                internalUrl = internalUrl.Replace(
                    $"{parsed.Scheme}://{parsed.Host}:{parsed.Port}",
                    $"{publicScheme}://{_options.PublicEndpoint}");
            }

            return internalUrl;
        }

        public async Task<Stream?> GetAsync(string key, CancellationToken ct = default)
        {
            await EnsureBucketExistsAsync(ct);
            var ms = new MemoryStream();
            try
            {
                var args = new GetObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(key)
                    .WithCallbackStream(stream => stream.CopyTo(ms));
                await _client.GetObjectAsync(args, ct);
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex) when (IsObjectNotFoundError(ex))
            {
                await ms.DisposeAsync();
                return null;
            }
        }

        public async Task<string> GetPresignedGetUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
        {
            await EnsureBucketExistsAsync(ct);

            var args = new PresignedGetObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key)
                .WithExpiry((int)expiry.TotalSeconds);

            var internalUrl = await _client.PresignedGetObjectAsync(args);

            if (!string.IsNullOrWhiteSpace(_options.PublicEndpoint) &&
                Uri.TryCreate(internalUrl, UriKind.Absolute, out var parsed))
            {
                var publicScheme = _options.UseSSL ? "https" : "http";
                internalUrl = internalUrl.Replace(
                    $"{parsed.Scheme}://{parsed.Host}:{parsed.Port}",
                    $"{publicScheme}://{_options.PublicEndpoint}");
            }

            return internalUrl;
        }

        private async Task EnsureBucketExistsAsync(CancellationToken ct)
        {
            if (_bucketEnsured) return;

            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.Bucket), ct);

            if (!exists)
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_options.Bucket), ct);

            _bucketEnsured = true;
        }

        // MinIO SDK v7 uses ObjectNotFoundException for missing objects/keys (NoSuchKey).
        // The S3 error code check is retained as a stable fallback.
        private static bool IsObjectNotFoundError(Exception ex) =>
            ex.GetType().Name is "ObjectNotFoundException" or "MinioException" ||
            ex.Message.Contains("NoSuchKey", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("NoSuchBucket", StringComparison.OrdinalIgnoreCase);
    }
}
