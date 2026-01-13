using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Infra.Log;
using IServiceProvider = Core.Infra.Service.IServiceProvider;

namespace Core.Services.Data
{
    public class RecordService : IRecordService
    {
        private readonly Dictionary<Type, BaseRecord> _records = new();
        
        public void Init(IServiceProvider provider)
        {
        }

        public void CreateAll()
        {
            var baseType = typeof(BaseRecord);

            var derivedTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in derivedTypes)
            {
                if (Activator.CreateInstance(type) is BaseRecord instance)
                {
                    _records.Add(instance.GetType(), instance);
                }
            }
        }

        public T GetRecord<T>() where T : BaseRecord
        {
            if (_records.TryGetValue(typeof(T), out var record))
            {
                return record as T;
            }
            
            Log.Error($"Could not find record of type {typeof(T).Name}, returning null.");
            return null;
        }
    }
}