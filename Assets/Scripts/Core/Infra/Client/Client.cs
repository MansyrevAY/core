using Core.Infra.Broadcaster;
using Core.Infra.Module;
using Core.Infra.Service;
using Core.Services.Asset;
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
        
        public static IClient Instance;

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
            AddService<IAssetService>(new AddressablesAssetService());
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

        protected void AddModule<T>(T module) where T : class, IModule
        {
            _moduleMapper.Add<T>(module); 
        }
        
        protected void AddModule<T, W>(T module) 
            where T : class, IModule
            where W : class, IInternalModule
        {
            _moduleMapper.Add<T>(module); 
            _internalModuleMapper.Add<W>(module as W);
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