using Core.Infra.Broadcaster;
using Core.Infra.Module;
using Core.Infra.Service;

namespace Core.Infra.Client
{
    public interface IClient : IServiceProvider, IModuleProvider, IInternalModuleProvider
    {
        public IBroadcaster Broadcaster { get; }
    }
}