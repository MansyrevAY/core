using Core.Infra.Broadcaster;
using Core.Infra.Client;

namespace Core.Infra.Module
{
    public abstract class BaseModule
    {
        protected readonly IClient Client;
        public readonly IBroadcaster Broadcaster;

        protected BaseModule(IClient client)
        {
            Client = client;
            Broadcaster = new Broadcaster.Broadcaster();
        }
    }
}