namespace Tycoon.Shared.Abstractions.Core.Domain
{
    /// <summary>
    /// Represents an entity that has audit information, including creation and last modification details.
    /// </summary>
    public interface IHaveAudit : IHaveCreator
    {
        DateTime? LastModified { get; }
        int? LastModifiedBy { get; }
    }
}
