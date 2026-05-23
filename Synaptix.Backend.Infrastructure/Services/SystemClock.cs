using Synaptix.Backend.Domain.Abstractions;

namespace Synaptix.Backend.Infrastructure.Services
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
