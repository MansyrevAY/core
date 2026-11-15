using Core.Infra.Broadcaster;
using Core.Infra.Module;
using Core.Infra.Service;

namespace Core.Infra.Client
{
    public interface IClient : IServiceProvider, IModuleProvider, IInternalModuleProvider
    {
        public IBroadcaster Broadcaster { get; }

        public void AddModule<T>(T module) where T : class, IModule;

        public void AddModule<T, W>(T module)
            where T : class, IModule
            where W : class, IInternalModule;

        public void RemoveModule<T>() where T : class, IModule;

        public void RemoveModule<T, W>()
            where T : class, IModule
            where W : class, IInternalModule;
    }
}