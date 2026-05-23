using Synaptix.Shared.Abstractions.Core.Messaging;

namespace Synaptix.Shared.Core.Messaging
{
    public abstract record IntegrationEvent : Message, IIntegrationEvent;
}

