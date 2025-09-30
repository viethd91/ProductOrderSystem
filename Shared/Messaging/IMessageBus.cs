using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    void Subscribe<T>(Func<T, Task> handler) where T : class;
}