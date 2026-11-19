using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Core.Infra.Lifetime
{
    public static class ReactiveExtensions
    {
        public static IDisposable EmptySubscribe<T>(this Observable<T> source)
        {
            return source.SafeSubscribe(_ => Observable.Empty<Unit>());
        }

        public static IDisposable SafeSubscribe<T>(this Observable<T> source, Action<T> action)
        {
            return source.Subscribe(value =>
            {
                try
                {
                    action(value);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            });
        }

        public static IDisposable SafeSubscribe<T>(this Observable<T> source, Action<T> onNext,
            Action<Result> onCompleted)
        {
            return source.Subscribe(value =>
            {
                try
                {
                    onNext(value);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }, onCompleted);
        }

        public static void SubscribeAsync<T>(this Observable<T> source, Func<T, UniTask> onNext, ref DisposableBag bag)
        {
            source.SelectMany(x => Observable.FromAsync(_ => onNext(x)))
                  .SafeSubscribe(_ => { }, LogIfFailed)
                  .AddTo(ref bag);
        }

        public static void SubscribeAsync<T>(this Observable<T> source, Func<T, CancellationToken, UniTask> onNext, ref DisposableBag bag)
        {
            source.SelectMany(x => Observable.FromAsync(ct => onNext(x, ct)))
                  .SafeSubscribe(_ => { }, LogIfFailed)
                  .AddTo(ref bag);
        }

        private static void LogIfFailed(Result result)
        {
            if (result.IsFailure)
                UnityEngine.Debug.LogException(result.Exception);
        }

        public static Observable<T> SkipInitialization<T>(this Observable<T> source)
        {
            return source.Skip(1);
        }

    }
}
