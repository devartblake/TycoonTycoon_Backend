using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Application.Analytics;

public sealed class NoopAnalyticsEventWriter : IAnalyticsEventWriter
{
    public Task UpsertQuestionAnsweredEventAsync(QuestionAnsweredAnalyticsEvent e, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public System.Threading.Tasks.Task WriteAsync(object evt, System.Threading.CancellationToken ct = default)
        => System.Threading.Tasks.Task.CompletedTask;
}
