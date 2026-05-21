using System.Security.Cryptography;

namespace Synaptix.Security.Kms.Application.Abstractions;

public interface ISecureSessionKeyExchange
{
    bool IsSupported(string suite);

    string SelectSuite(IReadOnlyList<string> supportedSuites);

    ECDiffieHellman CreatePrivateKey(string suite);

    byte[] ExportPublicKey(ECDiffieHellman privateKey);

    byte[] DeriveSharedSecret(ECDiffieHellman privateKey, byte[] peerPublicKey);
}
