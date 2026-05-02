namespace Synaptix.Security.Kms.Application.Abstractions;

public interface IReplayProtectionStore
{
    /// Returns true if the sequence + nonce pair is acceptable (first time seen within the window).
    /// Returns false if it was already seen (replay detected).
    Task<bool> TryAcceptAsync(
        Guid sessionId,
        long sequence,
        string nonce,
        TimeSpan ttl,
        CancellationToken ct);
}
