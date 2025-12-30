using Mediator;
using Tycoon.Shared.Observability.CoreDiagnostics.Commands;
using Tycoon.Shared.Observability.CoreDiagnostics.Query;
using IBaseCommand =Tycoon.Shared.Abstractions.Core.CQRS.IBaseCommand;
using IBaseQuery =Tycoon.Shared.Abstractions.Core.CQRS.IBaseQuery;

namespace Tycoon.Shared.Observability.Behaviors;

public class ObservabilityPipelineBehavior<TRequest, TResponse>(
    CommandHandlerActivity commandActivity,
    CommandHandlerMetrics commandMetrics,
    QueryHandlerActivity queryActivity,
    QueryHandlerMetrics queryMetrics
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : notnull
{
    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var isCommand = message is IBaseCommand;
        var isQuery = message is IBaseQuery;

        if (isCommand)
        {
            commandMetrics.StartExecuting<TRequest>();
        }

        if (isQuery)
        {
            queryMetrics.StartExecuting<TRequest>();
        }

        try
        {
            if (isCommand)
            {
                var commandResult = await commandActivity.Execute<TRequest, TResponse>(
                    async (activity, ct) =>
                    {
                        var response = await next(message, ct);

                        return response;
                    },
                    cancellationToken
                );

                commandMetrics.FinishExecuting<TRequest>();

                return commandResult;
            }

            if (isQuery)
            {
                var queryResult = await queryActivity.Execute<TRequest, TResponse>(
                    async (activity, ct) =>
                    {
                        var response = await next(message, ct);

                        return response;
                    },
                    cancellationToken
                );

                queryMetrics.FinishExecuting<TRequest>();

                return queryResult;
            }
        }
        catch (Exception)
        {
            if (isQuery)
            {
                queryMetrics.FailedCommand<TRequest>();
            }

            if (isCommand)
            {
                commandMetrics.FailedCommand<TRequest>();
            }

            throw;
        }

        return await next(message, cancellationToken);
    }
}
