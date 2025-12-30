using Tycoon.Shared.Abstractions.Core.Domain;

namespace Tycoon.Shared.Core.Exceptions
{
    public class BusinessRuleValidationException(IBusinessRule brokenRule) : DomainException(brokenRule.Message)
    {
        public IBusinessRule BrokenRule { get; } = brokenRule;
        public string Details { get; } = brokenRule.Message;

        public override string ToString()
        {
            return $"{BrokenRule.GetType().FullName}: {BrokenRule.Message}";
        }
    }
}
