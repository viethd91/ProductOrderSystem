using Products.API.Domain.Interfaces;

namespace Products.API.Domain.Events;

/// <summary>
/// Base class for domain events with common properties
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}