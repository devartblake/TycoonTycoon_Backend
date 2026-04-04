namespace Tycoon.Backend.Domain.Primitives
{
    /// <summary>
    /// Aggregate root base type. Domain event storage is inherited from <see cref="Entity"/>.
    /// </summary>
    public abstract class AggregateRoot : Entity
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public new IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected new void Raise(IDomainEvent evt) => _domainEvents.Add(evt);

        public new void ClearDomainEvents() => _domainEvents.Clear();
    }
}
