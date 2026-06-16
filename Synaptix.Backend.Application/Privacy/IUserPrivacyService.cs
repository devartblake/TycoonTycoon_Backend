namespace Synaptix.Backend.Application.Privacy;

public interface IUserPrivacyService
{
    Task AnonymizeUserAsync(Guid userId, CancellationToken ct = default);
    Task<string> ExportUserDataAsync(Guid userId, CancellationToken ct = default);
    Task ApplyOptOutAsync(Guid userId, CancellationToken ct = default);
}
