using System.Threading;
using System.Threading.Tasks;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Application.Analytics.Abstractions
{
    public interface IAnalyticsEventWriter
    {
        Task UpsertQuestionAnsweredEventAsync(QuestionAnsweredAnalyticsEvent e, CancellationToken ct);
    }
}
