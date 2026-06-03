using System.Security.Cryptography;
using System.Text;

namespace Synaptix.Setup.Secrets;

public static class SecretGenerator
{
    private const string AlphanumericChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string SafeSpecialChars = "!@#$%^&*-_+=";

    public static string GeneratePassword(int length = 32, bool includeSpecial = true)
    {
        var chars = includeSpecial
            ? AlphanumericChars + SafeSpecialChars
            : AlphanumericChars;

        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);
        foreach (var b in bytes)
            sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }

    public static string GenerateJwtKey(int bytes = 64) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

    public static string GenerateHex(int bytes = 32) =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();

    public static string GenerateApiKey(int length = 40) =>
        GeneratePassword(length, includeSpecial: false);
}
