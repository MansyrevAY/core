using System;
using Cysharp.Threading.Tasks;

namespace Core.Infra.Broadcaster
{
    public interface IBroadcaster
    {
        void Subscribe<T>(Action<T> callback) where T : Event;
        void Unsubscribe<T>(Action<T> callback) where T : Event;
        void Broadcast<T>(T message) where T : Event;
        
        void SubscribeAsync<T>(Func<T, UniTask> callback) where T : AsyncEvent;
        UniTask BroadcastAsync<T>(T message) where T : AsyncEvent;
    }
}