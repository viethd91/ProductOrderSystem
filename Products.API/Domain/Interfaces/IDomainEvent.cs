using MediatR;

namespace Products.API.Domain.Interfaces;

/// <summary>
/// Base interface for all domain events
/// Inherits from INotification to work with MediatR
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    DateTime OccurredOn { get; }
}