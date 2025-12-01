using AnalyzerCore.Domain.Abstractions;
using MediatR;

namespace AnalyzerCore.Application.Abstractions.Messaging;

/// <summary>
/// Handler for domain events.
/// </summary>
/// <typeparam name="TDomainEvent">The type of the domain event.</typeparam>
public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
}
