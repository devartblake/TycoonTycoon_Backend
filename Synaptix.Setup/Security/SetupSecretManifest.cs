using System.Text.Json;

namespace Synaptix.Setup.Security;

/// <summary>
/// Encrypted/protected setup secret manifest written to .local/bootstrap/bootstrap.secrets.enc.json.
/// In PlaintextLocal mode the file is unencrypted — gitignored, local-only.
/// In KMS modes the Ciphertext values are KMS-wrapped blobs.
/// </summary>
public sealed class SetupSecretManifest
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public int Version { get; init; } = 1;
    public string Environment { get; init; } = "local";
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, ProtectedSetupSecret> Secrets { get; init; } = [];

    public async Task SaveAsync(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, JsonOpts);
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task<SetupSecretManifest?> LoadAsync(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<SetupSecretManifest>(fs, JsonOpts);
        }
        catch { return null; }
    }
}
