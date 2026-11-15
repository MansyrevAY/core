using System.Collections.Generic;

namespace Core.Infra.Module
{
    // Owns the lifecycle of a group of modules (the whole client, or one scene).
    // It is not a registry: modules are built by hand, in dependency order, and nothing is looked up by type.
    public sealed class ModuleScope
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

        // Cleans modules up in reverse order, so dependents go before their dependencies.
        public void Cleanup()
        {
            for (var i = _modules.Count - 1; i >= 0; i--)
            {
                _modules[i].Cleanup();
            }

            _modules.Clear();
        }
    }
}
