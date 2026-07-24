using System.Security.Cryptography;

namespace Synaptix.Security.Kms.Client.Security;

/// <summary>
/// Certificate-pinning fingerprint helpers for the backend → KMS TLS channel.
/// A pin is the base64-encoded SHA-256 of the server leaf certificate's DER
/// (raw) bytes — the same shape the mobile client uses for its API pins.
/// </summary>
public static class KmsCertificatePinning
{
    /// <summary>Base64 SHA-256 of a certificate's raw DER bytes.</summary>
    public static string ComputePin(byte[] rawCertData)
        => Convert.ToBase64String(SHA256.HashData(rawCertData));

    /// <summary>
    /// True when <paramref name="rawCertData"/> hashes to one of <paramref name="pins"/>.
    /// Empty/whitespace pins are ignored; a null or empty pin set never matches.
    /// </summary>
    public static bool IsPinned(byte[] rawCertData, IEnumerable<string>? pins)
    {
        if (pins is null) return false;
        var fingerprint = ComputePin(rawCertData);
        foreach (var pin in pins)
        {
            if (!string.IsNullOrWhiteSpace(pin) &&
                string.Equals(pin.Trim(), fingerprint, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
}
