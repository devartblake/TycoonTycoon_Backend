using System.Net.Http.Headers;

namespace Tycoon.Backend.Api.Tests.TestHost
{
    public static class HttpClientAdminExtensions
    {
        public static HttpClient WithAdminOpsKey(this HttpClient client, string key = "test-ops-key")
        {
            client.DefaultRequestHeaders.Remove("X-Admin-Ops-Key");
            client.DefaultRequestHeaders.Add("X-Admin-Ops-Key", key);

            // Optional: admin identity for audit
            client.DefaultRequestHeaders.Remove("X-Admin-User");
            client.DefaultRequestHeaders.Add("X-Admin-User", "test-admin");

            return client;
        }
    }
}
