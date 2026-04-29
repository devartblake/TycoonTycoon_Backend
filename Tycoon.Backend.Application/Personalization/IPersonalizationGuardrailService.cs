using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Personalization;

public interface IPersonalizationGuardrailService
{
    PersonalizationGuardrailResult Apply(PlayerMindProfileDto profile, PersonalizationCandidateDto candidate);
}
