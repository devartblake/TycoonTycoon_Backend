namespace Tycoon.Backend.Application.Abstractions
{
    /// <summary>
    /// Implemented by storage backends that support client-side direct uploads
    /// via time-limited presigned PUT URLs (e.g. MinIO, S3).
    /// </summary>
    public interface IPresignedStorage
    {
        /// <summary>
        /// Generates a presigned HTTP PUT URL that an external client (e.g. browser)
        /// can use to upload a file directly to the storage backend without routing
        /// the bytes through the API server.
        /// </summary>
        Task<string> GetPresignedPutUrlAsync(string key, string contentType, TimeSpan expiry, CancellationToken ct = default);

        /// <summary>
        /// Generates a presigned HTTP GET URL that allows a client to download an object
        /// directly from storage without routing the bytes through the API server.
        /// </summary>
        Task<string> GetPresignedGetUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    }
}
