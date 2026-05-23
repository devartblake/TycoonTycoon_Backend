using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Personalization;

public interface IPersonalizationGuardrailService
{
    PersonalizationGuardrailResult Apply(PlayerMindProfileDto profile, PersonalizationCandidateDto candidate);
}
