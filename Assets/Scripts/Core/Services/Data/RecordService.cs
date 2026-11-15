using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Infra.Log;
using Core.Services.Asset;
using Cysharp.Threading.Tasks;

namespace Core.Services.Data
{
    public class RecordService : IRecordService
    {
        private const string RecordsStorageName = "RecordsStorage";

        private readonly Dictionary<Type, BaseRecord> _records = new();

        private readonly IAssetService _assetService;

        public RecordService(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public UniTask CreateAll()
        {
            _records.Clear();

            foreach (var type in FindRecordTypes())
            {
                if (Activator.CreateInstance(type) is BaseRecord instance)
                {
                    _records.Add(type, instance);
                }
            }

            return LoadRecords();
        }

        // Records live in the game's assemblies, not in Core, so scan Core itself plus every
        // loaded assembly that references it instead of relying on a hardcoded assembly name.
        private static IEnumerable<Type> FindRecordTypes()
        {
            var baseType = typeof(BaseRecord);
            var coreAssembly = baseType.Assembly;
            var coreName = coreAssembly.GetName().Name;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                var referencesCore = assembly == coreAssembly ||
                                     assembly.GetReferencedAssemblies().Any(a => a.Name == coreName);

                if (!referencesCore)
                {
                    continue;
                }

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (baseType.IsAssignableFrom(type) && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                    {
                        yield return type;
                    }
                }
            }
        }

        // Authored values from the storage asset override the code defaults created in CreateAll.
        private async UniTask LoadRecords()
        {
            var storage = await _assetService.Load<RecordStorage>(RecordsStorageName);

            if (storage == null || storage.Records == null)
            {
                Log.Warning($"Record storage '{RecordsStorageName}' not found, using default records");
                return;
            }

            foreach (var recordObject in storage.Records)
            {
                if (recordObject == null || recordObject.Record == null)
                {
                    Log.Warning($"Empty record entry in '{RecordsStorageName}', ignoring");
                    continue;
                }

                var type = recordObject.Record.GetType();

                if (!_records.ContainsKey(type))
                {
                    Log.Error($"Missing record {type.Name} in the service storage, ignoring");
                    continue;
                }

                _records[type] = recordObject.Record;
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