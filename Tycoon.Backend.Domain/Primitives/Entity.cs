namespace Tycoon.Backend.Domain.Primitives
{
    /// <summary>
    /// Base type for domain entities. Uses Guid identity.
    /// Pure domain: no EF attributes.
    /// </summary>
    public abstract class Entity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected void Raise(IDomainEvent @event)
            => _domainEvents.Add(@event);

        public void ClearDomainEvents()
            => _domainEvents.Clear();
    }
}
