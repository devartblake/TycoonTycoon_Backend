using Tycoon.Shared.Abstractions.Core.Messaging;

namespace Tycoon.Shared.Core.Messaging
{
    public abstract record IntegrationEvent : Message, IIntegrationEvent;
}

