using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Infra
{
    public class InterfaceInstanceMapper<T>
    {
        private readonly Dictionary<Type, T> _classToInterfaceInstance = new();

        // Add mapping from TClass to an interface instance
        public void Add<TClass>(T interfaceInstance)
            where TClass : class
        {
            if (interfaceInstance == null)
                throw new ArgumentNullException(nameof(interfaceInstance));

            var classType = typeof(TClass);

            if (!typeof(T).IsAssignableFrom(classType))
                throw new ArgumentException($"Class {classType.FullName} is not assignable from {typeof(T).FullName}");

            if (!_classToInterfaceInstance.TryAdd(classType, interfaceInstance))
                throw new InvalidOperationException($"Class {classType.Name} is already mapped.");
        }

        // Delete mapping by class
        public void Remove<TClass>()
            where TClass : class
        {
            var classType = typeof(TClass);

            if (!_classToInterfaceInstance.Remove(classType, out _))
                throw new KeyNotFoundException($"Class {classType.Name} not found.");
        }
    
        // Replace interface instance for a class
        public void Replace<TClass>(T newInstance)
            where TClass : class
        {
            if (newInstance == null)
                throw new ArgumentNullException(nameof(newInstance));

            var classType = typeof(TClass);

            if (!_classToInterfaceInstance.TryGetValue(classType, out _))
            {
                throw new KeyNotFoundException($"Class {classType.Name} not found.");
            }

            if (!newInstance.GetType().IsInterface && newInstance.GetType().GetInterfaces().Length == 0)
            {
                throw new ArgumentException("Replacement must be an interface instance.");
            }

            _classToInterfaceInstance[classType] = newInstance;
        }

        // Get interface instance with a typed cast
        public TClass Get<TClass>()
            where TClass : class
        {
            var classType = typeof(TClass);

            if (!_classToInterfaceInstance.TryGetValue(classType, out var instance))
            {
                throw new KeyNotFoundException($"Class {classType.Name} not found.");
            }

            return instance as TClass;
        }

        public List<T> All => _classToInterfaceInstance.Values.ToList();
    }
}