using Microsoft.Extensions.Logging;
using Synaptix.Compliance.Client.Abstractions;

namespace Synaptix.Backend.Application.Privacy;

public sealed class PrivacyRequestFulfillmentJob(
    IComplianceClient compliance,
    IUserPrivacyService privacy,
    ILogger<PrivacyRequestFulfillmentJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Synaptix.Compliance.Client.Models.Responses.PendingPrivacyRequestItem> pending;
        try
        {
            pending = await compliance.GetPendingPrivacyRequestsAsync(limit: 20, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch pending privacy requests from compliance service");
            return;
        }

        foreach (var request in pending)
        {
            string? notes = null;
            try
            {
                switch (request.RequestType.ToUpperInvariant())
                {
                    case "DELETE":
                        await privacy.AnonymizeUserAsync(request.UserId, ct);
                        notes = "User PII anonymized; financial records preserved.";
                        break;

                    case "KNOW":
                    case "DATAPORTABILITY":
                        var exportUrl = await privacy.ExportUserDataAsync(request.UserId, ct);
                        notes = $"Data export available at: {exportUrl}";
                        break;

                    case "OPTOUT":
                        await privacy.ApplyOptOutAsync(request.UserId, ct);
                        notes = "Marketing opt-out recorded.";
                        break;

                    default:
                        logger.LogWarning("Unknown privacy request type '{Type}' for request {Id}",
                            request.RequestType, request.Id);
                        notes = $"Unrecognised request type: {request.RequestType}";
                        break;
                }

                await compliance.CompletePrivacyRequestAsync(request.Id, "Completed", notes, ct);
                logger.LogInformation("Privacy request {Id} ({Type}) completed for user {UserId}",
                    request.Id, request.RequestType, request.UserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process privacy request {Id} for user {UserId}",
                    request.Id, request.UserId);
                try
                {
                    await compliance.CompletePrivacyRequestAsync(
                        request.Id, "Failed", $"Error: {ex.Message}", ct);
                }
                catch
                {
                    // best effort status update
                }
            }
        }
    }
}
