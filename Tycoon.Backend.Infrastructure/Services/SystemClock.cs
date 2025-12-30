using Tycoon.Backend.Domain.Abstractions;

namespace Tycoon.Backend.Infrastructure.Services
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
