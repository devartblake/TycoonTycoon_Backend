namespace Tycoon.Backend.Domain.Abstractions
{
    /// <summary>
    /// Provides current time. Abstracted for testability.
    /// Domain-level abstraction used by Application and Infrastructure.
    /// </summary>
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}