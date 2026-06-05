using System.Threading;
using System.Threading.Tasks;
using Synaptix.Backend.Application.Analytics.Models;

namespace Synaptix.Backend.Application.Analytics.Abstractions
{
    public interface IAnalyticsEventWriter
    {
        Task<bool> UpsertQuestionAnsweredEventAsync(QuestionAnsweredAnalyticsEvent e, CancellationToken ct);
    }
}
