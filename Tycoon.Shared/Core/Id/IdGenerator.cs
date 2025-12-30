namespace Tycoon.Shared.Core.Id
{
    /// <summary>
    /// Static helper class for Id generation.
    /// </summary>
    public static class IdGenerator
    {
        public static Guid NewId()
        {
            return MassTransit.NewId.NextGuid();
        }
    }
}
