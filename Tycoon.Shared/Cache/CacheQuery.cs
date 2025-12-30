using Tycoon.Shared.Abstractions.Caching;
using Tycoon.Shared.Abstractions.Core.CQRS;

namespace Tycoon.Shared.Cache
{
    public abstract record CacheQuery<TRequest, TResponse> : ICacheQuery<TRequest, TResponse>
        where TRequest : IQuery<TResponse>
        where TResponse : class
    {
        public virtual TimeSpan? AbsoluteExpirationRelativeToNow { get; }
        public virtual TimeSpan? AbsoluteLocalCacheExpirationRelativeToNow { get; }
        public virtual string Prefix => "Ch_";

        public virtual string CacheKey(TRequest request) => $"{Prefix}{typeof(TRequest).Name}";
    }
}
