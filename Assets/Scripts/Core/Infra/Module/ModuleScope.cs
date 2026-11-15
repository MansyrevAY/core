using System;
using System.Collections.Generic;
using Core.Infra.Logging;

namespace Core.Infra.Module
{
    public sealed class ModuleScope : IDisposable
    {
        private readonly List<IModule> _modules = new();

        public T Add<T>(T module) where T : class, IModule
        {
            _modules.Add(module);
            return module;
        }

        public void Start()
        {
            foreach (var module in _modules)
            {
                module.Start();
            }
        }

        public void Dispose()
        {
            for (var i = _modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _modules[i].Dispose();
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            _modules.Clear();
        }
    }
}
