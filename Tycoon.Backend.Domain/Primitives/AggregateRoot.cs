namespace Tycoon.Backend.Domain.Primitives
{
    /// <summary>
    /// Aggregate root base type. Holds domain events raised by aggregates.
    /// </summary>
    public abstract class AggregateRoot : Entity
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected void Raise(IDomainEvent evt) => _domainEvents.Add(evt);

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
