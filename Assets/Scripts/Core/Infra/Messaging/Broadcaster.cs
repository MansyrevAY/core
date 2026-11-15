using System;
using System.Collections.Generic;
using Core.Infra.Logging;
using Cysharp.Threading.Tasks;

namespace Core.Infra.Messaging
{
    public class Broadcaster : IBroadcaster
    {
        // Copy-on-write subscriber arrays: handlers can subscribe or unsubscribe during dispatch.
        private readonly Dictionary<Type, Delegate[]> _subscribers = new();

        public void Subscribe<T>(Action<T> callback) where T : Event
        {
            Add(typeof(T), callback);
        }

        public void Unsubscribe<T>(Action<T> callback) where T : Event
        {
            Remove(typeof(T), callback);
        }

        public void Broadcast<T>(T message) where T : Event
        {
            if (!_subscribers.TryGetValue(typeof(T), out var subscribers))
            {
                return;
            }

            foreach (var subscriber in subscribers)
            {
                try
                {
                    (subscriber as Action<T>)?.Invoke(message);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }

        public void SubscribeAsync<T>(Func<T, UniTask> callback) where T : AsyncEvent
        {
            Add(typeof(T), callback);
        }

        public void UnsubscribeAsync<T>(Func<T, UniTask> callback) where T : AsyncEvent
        {
            Remove(typeof(T), callback);
        }

        public async UniTask BroadcastAsync<T>(T message) where T : AsyncEvent
        {
            if (!_subscribers.TryGetValue(typeof(T), out var subscribers))
            {
                return;
            }

            foreach (var subscriber in subscribers)
            {
                if (subscriber is not Func<T, UniTask> action)
                {
                    continue;
                }

                try
                {
                    await action(message);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }

        private void Add(Type type, Delegate callback)
        {
            if (!_subscribers.TryGetValue(type, out var current))
            {
                _subscribers[type] = new[] { callback };
                return;
            }

            var next = new Delegate[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = callback;
            _subscribers[type] = next;
        }

        private void Remove(Type type, Delegate callback)
        {
            if (!_subscribers.TryGetValue(type, out var current))
            {
                return;
            }

            var index = Array.IndexOf(current, callback);

            if (index < 0)
            {
                return;
            }

            if (current.Length == 1)
            {
                _subscribers.Remove(type);
                return;
            }

            var next = new Delegate[current.Length - 1];
            Array.Copy(current, 0, next, 0, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            _subscribers[type] = next;
        }
    }
}
