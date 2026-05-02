using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Application.Abstractions;

public interface IKeyWrappingService
{
    Task<WrappedDataKey> GenerateDataKeyAsync(string keyName, CancellationToken ct);
    Task<byte[]> UnwrapDataKeyAsync(string keyName, string encryptedDataKey, CancellationToken ct);
}
