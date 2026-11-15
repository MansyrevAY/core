using Core.Infra.Broadcaster;

namespace Core.Infra.Module
{
    public abstract class BaseModule
    {
        // The module's own event bus, exposed to its views through IInternalModule.
        public IBroadcaster Broadcaster { get; } = new Broadcaster.Broadcaster();
    }
}
