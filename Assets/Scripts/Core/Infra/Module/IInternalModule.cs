using Core.Infra.Messaging;

namespace Core.Infra.Module
{
    public interface IInternalModule
    {
        IBroadcaster Broadcaster { get; }
    }
}