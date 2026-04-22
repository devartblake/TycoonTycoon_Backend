using Minio;
using Minio.DataModel.Args;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Infrastructure.Storage
{
    public sealed class MinioObjectStorage : IObjectStorage, IPresignedStorage
    {
        private readonly IMinioClient _client;
        private readonly MinioOptions _options;

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
                    .WithCallbackStream(async stream => await stream.CopyToAsync(ms, ct));
                await _client.GetObjectAsync(args, ct);
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex) when (ex.Message.Contains("NoSuchKey") || ex.Message.Contains("Not Found") || ex.GetType().Name.Contains("ObjectNotFound"))
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
            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.Bucket), ct);

            if (!exists)
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_options.Bucket), ct);
        }
    }
}
