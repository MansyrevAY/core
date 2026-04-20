using UnityEngine;

namespace Core.Services.Asset
{
    public class DefaultInstance<T> : IInstance<T> where T : Object
    {
        public string Id { get; }
        public T Value { get; }

        public DefaultInstance(string id, T instance)
        {
            Id = id;
            Value = instance;
        }
    }
}