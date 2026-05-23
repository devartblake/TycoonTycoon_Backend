using Synaptix.Shared.Abstractions.Core.Paging;

namespace Synaptix.Shared.Abstractions.Core.CQRS
{
    public interface IPageQuery<out TResponse> : IPageRequest, IQuery<TResponse>
    where TResponse : class;
}
