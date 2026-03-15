namespace Tycoon.Backend.Infrastructure.Storage
{
    public sealed class MinioOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Bucket { get; set; } = "tycoon-assets";
        public bool UseSSL { get; set; } = false;

        /// <summary>
        /// Optional override for the public-facing URL base (e.g. localhost:9000 in dev
        /// vs the internal minio:9000 the API uses to connect). Falls back to Endpoint.
        /// </summary>
        public string? PublicEndpoint { get; set; }
    }
}
