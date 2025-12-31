using System.Net.Http.Headers;

namespace Tycoon.Backend.Api.Tests.TestHost
{
    public static class AdminClient
    {
        public static HttpClient WithAdminOpsKey(this HttpClient client)
        {
            client.DefaultRequestHeaders.Remove("X-Admin-Ops-Key");
            client.DefaultRequestHeaders.Add("X-Admin-Ops-Key", TycoonApiFactory.TestAdminKey);
            return client;
        }
    }
}
