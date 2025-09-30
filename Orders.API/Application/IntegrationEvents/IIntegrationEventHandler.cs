namespace Orders.API.Application.IntegrationEvents;

/// <summary>
/// Interface for handling integration events from other bounded contexts
/// </summary>
/// <typeparam name="T">Type of integration event to handle</typeparam>
public interface IIntegrationEventHandler<in T> where T : class
{
    /// <summary>
    /// Handles the integration event asynchronously
    /// </summary>
    /// <param name="event">The integration event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}