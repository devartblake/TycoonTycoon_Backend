using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Privacy;

namespace Synaptix.Backend.Api.Features.AdminPrivacy;

public static class AdminPrivacyEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/privacy-requests").WithTags("Admin/Privacy");

        g.MapPost("/{requestId:guid}/process", ProcessRequest);
    }

    private static async Task<IResult> ProcessRequest(
        [FromRoute] Guid requestId,
        [FromBody] AdminProcessPrivacyRequest body,
        IUserPrivacyService privacy,
        Synaptix.Compliance.Client.Abstractions.IComplianceClient compliance,
        CancellationToken ct)
    {
        string? notes;
        try
        {
            switch (body.RequestType.ToUpperInvariant())
            {
                case "DELETE":
                    await privacy.AnonymizeUserAsync(body.UserId, ct);
                    notes = "User PII anonymized by admin.";
                    break;

                case "KNOW":
                case "DATAPORTABILITY":
                    var url = await privacy.ExportUserDataAsync(body.UserId, ct);
                    notes = $"Data export: {url}";
                    break;

                case "OPTOUT":
                    await privacy.ApplyOptOutAsync(body.UserId, ct);
                    notes = "Marketing opt-out applied by admin.";
                    break;

                default:
                    return ApiResponses.Error(StatusCodes.Status400BadRequest,
                        "INVALID_REQUEST_TYPE", $"Unknown requestType '{body.RequestType}'.");
            }

            await compliance.CompletePrivacyRequestAsync(requestId, "Completed", notes, ct);
            return Results.Ok(new { requestId, status = "Completed", notes });
        }
        catch (Exception ex)
        {
            return ApiResponses.Error(StatusCodes.Status500InternalServerError,
                "PROCESSING_FAILED", ex.Message);
        }
    }
}

internal sealed record AdminProcessPrivacyRequest(Guid UserId, string RequestType);
