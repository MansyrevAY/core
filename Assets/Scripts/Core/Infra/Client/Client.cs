using Core.Infra.Broadcaster;
using Core.Infra.Module;
using Core.Infra.Service;
using Core.Services.Asset;
using Core.Services.Asset.Resource;
using Core.Services.Data;
using UnityEngine;

namespace Core.Infra.Client
{
    public class Client : MonoBehaviour, IClient
    {
        private InterfaceInstanceMapper<IService> _serviceMapper;
        private InterfaceInstanceMapper<IModule> _moduleMapper;
        private InterfaceInstanceMapper<IInternalModule> _internalModuleMapper;
        
        public IBroadcaster Broadcaster { get; private set; }

        private void Awake()
        {
            _serviceMapper = new InterfaceInstanceMapper<IService>();
            _moduleMapper = new InterfaceInstanceMapper<IModule>();
            _internalModuleMapper = new InterfaceInstanceMapper<IInternalModule>();
            
            Broadcaster = new Broadcaster.Broadcaster();
            
            CreateServices();
            CreateModules();
            
            InitServices();
            InitModules();
            StartGame();
        }

        protected virtual void CreateServices()
        {
            AddService<IAssetService>(new ResourcesAssetService());
            AddService<IRecordService>(new RecordService());
        }

        protected virtual void CreateModules()
        {
        }

        private void InitServices()
        {
            foreach (var service in _serviceMapper.All)
            {
                service.Init(this);
            }
        }

        private void InitModules()
        {
            foreach (var module in _moduleMapper.All)
            {
                module.Init(this);
            }
        }

        protected void AddService<T>(T service) where T : class, IService
        {
            _serviceMapper.Add<T>(service); 
        }

        public void AddModule<T>(T module) where T : class, IModule
        {
            _moduleMapper.Add<T>(module); 
        }
        
        public void AddModule<T, W>(T module)
            where T : class, IModule
            where W : class, IInternalModule
        {
            _moduleMapper.Add<T>(module); 
            _internalModuleMapper.Add<W>(module as W); // TODO: look at internals again
        }

        public void RemoveModule<T>() where T : class, IModule
        {
            var module = _moduleMapper.Get<T>();
            module.Cleanup(this);
            
            _moduleMapper.Remove<T>();
        }

        public void RemoveModule<T, W>() where T : class, IModule where W : class, IInternalModule
        {
            RemoveModule<T>();
            _internalModuleMapper.Remove<W>();
        }

        public T GetService<T>() where T : class, IService
        {
            return _serviceMapper.Get<T>();
        }

        protected virtual void StartGame()
        {
            
        }

        T IModuleProvider.GetModule<T>()
        {
            return _moduleMapper.Get<T>();
        }

        T IInternalModuleProvider.GetModule<T>()
        {
            return _internalModuleMapper.Get<T>();
        }
    }
}