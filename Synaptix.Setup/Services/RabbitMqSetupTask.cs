using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Synaptix.Setup.Services;

// Uses the RabbitMQ HTTP Management API (port 15672) to avoid AMQP client dependency.
public sealed class RabbitMqSetupTask : ISetupTask
{
    public string Name => "RabbitMQ";

    public async Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var host       = cfg["RABBITMQ_HOST"]        ?? cfg["RabbitMQ:Host"]           ?? "localhost";
        var mgmtPort   = cfg["RABBITMQ_MANAGEMENT_PORT"] ?? cfg["RabbitMQ:ManagementPort"] ?? "15672";
        var adminUser  = cfg["RABBITMQ_USER"]        ?? cfg["RabbitMQ:User"]           ?? "synaptix_user";
        var adminPass  = cfg["RABBITMQ_PASSWORD"]    ?? cfg["RabbitMQ:Password"];
        var vhost      = cfg["RABBITMQ_VHOST"]       ?? cfg["RabbitMQ:Vhost"]          ?? "synaptix";

        if (string.IsNullOrWhiteSpace(adminPass))
            return SetupResult.Fail("RABBITMQ_PASSWORD is not set.");

        var baseUrl = $"http://{host}:{mgmtPort}/api";
        using var client = CreateHttpClient(adminUser, adminPass);

        try
        {
            // 1. Verify management API is reachable
            var overview = await client.GetAsync($"{baseUrl}/overview", ct);
            if (!overview.IsSuccessStatusCode)
                return SetupResult.Fail($"RabbitMQ management API returned {(int)overview.StatusCode}.");
            Log.Information("RabbitMQ management API reachable.");

            // 2. Ensure vhost exists
            var encodedVhost = Uri.EscapeDataString(vhost);
            var vhostResp = await client.PutAsync($"{baseUrl}/vhosts/{encodedVhost}",
                new StringContent("{}", Encoding.UTF8, "application/json"), ct);
            Log.Information("RabbitMQ vhost '{Vhost}' ensured (status: {Status}).", vhost, (int)vhostResp.StatusCode);

            // 3. Grant permissions to the default user on the vhost
            var permPayload = JsonSerializer.Serialize(new { configure = ".*", write = ".*", read = ".*" });
            var permResp = await client.PutAsync(
                $"{baseUrl}/permissions/{encodedVhost}/{Uri.EscapeDataString(adminUser)}",
                new StringContent(permPayload, Encoding.UTF8, "application/json"), ct);
            Log.Information("RabbitMQ permissions set for '{User}' on '{Vhost}' (status: {Status}).",
                adminUser, vhost, (int)permResp.StatusCode);

            // 4. Ensure durable background jobs queue
            var queuePayload = JsonSerializer.Serialize(new { durable = true, auto_delete = false, arguments = new { } });
            await client.PutAsync(
                $"{baseUrl}/queues/{encodedVhost}/synaptix.background.jobs",
                new StringContent(queuePayload, Encoding.UTF8, "application/json"), ct);

            return SetupResult.Ok($"RabbitMQ provisioned — vhost: '{vhost}', user: '{adminUser}'.");
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "RabbitMQ setup failed.");
            return SetupResult.Fail($"RabbitMQ unreachable: {ex.Message}");
        }
    }

    private static HttpClient CreateHttpClient(string user, string pass)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return client;
    }
}
