using Minio;
using Minio.DataModel.Args;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Infrastructure.Storage
{
    public sealed class MinioObjectStorage : IObjectStorage
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
