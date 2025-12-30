using MassTransit;

namespace Tycoon.Shared.Abstractions.Core.Messaging
{
    public interface IMessageHandler<in TMessage> : IConsumer<TMessage>
        where TMessage : class, IMessage;
}
