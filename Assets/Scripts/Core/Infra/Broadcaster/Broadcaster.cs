using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Core.Infra.Broadcaster
{
    public class Broadcaster : IBroadcaster
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        
        public void Subscribe<T>(Action<T> callback) where T : Event
        {
            var type = typeof(T);

            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }
            
            _subscribers[type].Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback) where T : Event
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var subscriber))
            {
                return;
            }
            
            subscriber.Remove(callback);
        }

        public void Broadcast<T>(T message) where T : Event
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var subscribers))
            {
                return;
            }

            foreach (var subscriber in subscribers)
            {
                var action = subscriber as Action<T>;
                action?.Invoke(message);
            }
        }

        public void SubscribeAsync<T>(Func<T, UniTask> callback) where T : AsyncEvent
        {
            var type = typeof(T);

            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }
            
            _subscribers[type].Add(callback);
        }

        public UniTask BroadcastAsync<T>(T message) where T : AsyncEvent
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var subscribers))
            {
                return UniTask.CompletedTask;
            }
            
            foreach (var subscriber in subscribers)
            {
                if (subscriber is Func<T, UniTask> action)
                {
                    action(message);
                }
            }
            
            return UniTask.CompletedTask;
        }
    }
}