namespace Tycoon.Shared.Abstractions.Core.Domain
{
    /// <summary>
    /// Represents an entity that has creator information, including creation timestamp and creator identifier.
    /// </summary>
    public interface IHaveCreator
    {
        DateTime Created { get; }
        int? CreatedBy { get; }
    }
}
