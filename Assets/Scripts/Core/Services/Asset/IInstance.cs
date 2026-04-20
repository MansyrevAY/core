using UnityEngine;

namespace Core.Services.Asset
{
    public interface IInstance<out T> where T : Object
    {
        public string Id { get; }
        public T Value { get; }
    }
}