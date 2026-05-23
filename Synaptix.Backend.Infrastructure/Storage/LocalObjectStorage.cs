using Synaptix.Backend.Application.Abstractions;

namespace Synaptix.Backend.Infrastructure.Storage
{
    /// <summary>
    /// Fallback implementation that writes files to wwwroot on the local disk.
    /// Used automatically when MinIO is not configured (local dev, tests).
    /// </summary>
    public sealed class LocalObjectStorage : IObjectStorage
    {
        private readonly string _root;

        public LocalObjectStorage(string? root = null)
        {
            _root = root ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        }

        public async Task PutAsync(string key, Stream content, string contentType, long size = -1, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using var fs = File.Create(fullPath);
            await content.CopyToAsync(fs, ct);
        }

        public string GetPublicUrl(string key) => $"/{key}";

        public Task<Stream?> GetAsync(string key, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
                return Task.FromResult<Stream?>(null);

            return Task.FromResult<Stream?>(File.OpenRead(fullPath));
        }
    }
}
