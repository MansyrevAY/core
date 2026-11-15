using Core.Infra.Messaging;

namespace Core.Infra.Module
{
    public abstract class BaseModule
    {
        public IBroadcaster Broadcaster { get; } = new Broadcaster();
    }
}
