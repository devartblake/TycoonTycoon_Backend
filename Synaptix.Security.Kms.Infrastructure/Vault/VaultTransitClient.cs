using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Synaptix.Security.Kms.Infrastructure.Vault;

/// Thin HTTP client for the Vault Transit secrets engine.
/// Uses the Vault REST API directly to avoid adding VaultSharp as a dependency.
public sealed class VaultTransitClient(
    HttpClient http,
    IOptions<VaultOptions> opts)
{
    private readonly VaultOptions _opts = opts.Value;

    /// Asks Vault Transit to generate a data encryption key.
    /// Returns plaintext key bytes + the ciphertext (wrapped) key for storage.
    public async Task<(byte[] PlaintextKey, string CiphertextKey)> GenerateDataKeyAsync(
        string keyName, CancellationToken ct)
    {
        var url = $"/v1/{_opts.TransitMount}/datakey/plaintext/{keyName}";
        var response = await http.PostAsync(url, null, ct);
        await EnsureSuccessAsync(response, "generate-datakey", ct);

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var data = doc.RootElement.GetProperty("data");
        var plaintextB64 = data.GetProperty("plaintext").GetString()!;
        var ciphertext = data.GetProperty("ciphertext").GetString()!;

        return (Convert.FromBase64String(plaintextB64), ciphertext);
    }

    /// Asks Vault Transit to decrypt (unwrap) a previously generated data key.
    public async Task<byte[]> DecryptDataKeyAsync(
        string keyName, string ciphertextKey, CancellationToken ct)
    {
        var url = $"/v1/{_opts.TransitMount}/decrypt/{keyName}";
        var body = JsonSerializer.Serialize(new { ciphertext = ciphertextKey });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        var response = await http.PostAsync(url, content, ct);
        await EnsureSuccessAsync(response, "decrypt-datakey", ct);

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var plaintextB64 = doc.RootElement
            .GetProperty("data")
            .GetProperty("plaintext")
            .GetString()!;

        return Convert.FromBase64String(plaintextB64);
    }

    /// Returns the latest key version string for the named Transit key.
    public async Task<string> GetLatestKeyVersionAsync(string keyName, CancellationToken ct)
    {
        var url = $"/v1/{_opts.TransitMount}/keys/{keyName}";
        var response = await http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, "get-key-version", ct);

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var latestVersion = doc.RootElement
            .GetProperty("data")
            .GetProperty("latest_version")
            .GetInt32();

        return $"v{latestVersion}";
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(
            $"Vault Transit {operation} failed with {(int)response.StatusCode}: {body}");
    }
}
