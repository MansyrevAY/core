using System;
using System.Collections.Generic;
using Core.Infra.Logging;

namespace Core.Infra.Module
{
    // Owns the lifecycle of a group of modules (the whole client, or one scene).
    // It is not a registry: modules are built by hand, in dependency order, and nothing is looked up by type.
    public sealed class ModuleScope : IDisposable
    {
        private readonly List<IModule> _modules = new();

        public T Add<T>(T module) where T : class, IModule
        {
            _modules.Add(module);
            return module;
        }

        // Starts modules in the order they were added.
        public void Start()
        {
            foreach (var module in _modules)
            {
                module.Start();
            }
        }

        // Disposes modules in reverse order, so dependents go before their dependencies.
        // A module that throws is logged and does not stop the rest from being disposed.
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
