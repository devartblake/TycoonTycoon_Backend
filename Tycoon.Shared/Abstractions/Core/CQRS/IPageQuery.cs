using Tycoon.Shared.Abstractions.Core.Paging;

namespace Tycoon.Shared.Abstractions.Core.CQRS
{
    public interface IPageQuery<out TResponse> : IPageRequest, IQuery<TResponse>
    where TResponse : class;
}
