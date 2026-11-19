using Core.Resources;
using Core.Infra.Lifetime;
using System;
using Fusion;
using R3;
using UnityEngine;


namespace Core.Networking.Fusion
{
    public sealed class SharedAuthorityLease : IDisposable
    {
        private const int RetryIntervalFrames = 30;

        private readonly NetworkBehaviour _behaviour;
        private readonly Action _onAcquired;
        private readonly Action _onLost;
        private readonly Func<bool> _canClaim;

        private bool _started;
        private bool _hadAuthority;
        private int _nextRequestFrame;
        public bool IsAcquired => HasAuthority();

        public SharedAuthorityLease(NetworkBehaviour behaviour, Action onAcquired, Action onLost = null, Func<bool> canClaim = null)
        {
            _behaviour = behaviour;
            _onAcquired = onAcquired;
            _onLost = onLost;
            _canClaim = canClaim;
        }

        public void Start(ref DisposableBag disposables)
        {
            if (_started)
                return;

            _started = true;
            _hadAuthority = false;
            Evaluate();

            Observable.EveryUpdate()
                .SafeSubscribe(_ => Evaluate())
                .AddTo(ref disposables);
        }

        public void NotifyFusionAuthorityChanged() => Evaluate();

        private void Evaluate()
        {
            if (!_started || !_behaviour || !_behaviour.Object)
                return;

            var hasAuthority = HasAuthority();
            if (hasAuthority != _hadAuthority)
            {
                _hadAuthority = hasAuthority;
                if (hasAuthority)
                {
                    var authority = _behaviour.Object.StateAuthority;
                    var authorityPlayer = authority.IsMasterClient
                        ? _behaviour.Runner.GetMasterClient()
                        : authority;
                    Debug.Log($"[SharedAuthority] acquired: object={_behaviour.name} id={_behaviour.Object.Id}, " +
                              $"stateAuthority={authorityPlayer}, inputAuthority={_behaviour.Object.InputAuthority}, " +
                              $"local={_behaviour.Runner.LocalPlayer}");
                    _onAcquired?.Invoke();
                }
                else
                    _onLost?.Invoke();
            }

            if (!_behaviour.Runner || !_behaviour.Runner.IsRunning || _behaviour.Runner.Topology != Topologies.Shared)
                return;

            if (Time.frameCount < _nextRequestFrame)
                return;

            if (hasAuthority)
            {
                if (_canClaim != null && !_canClaim())
                {
                    _nextRequestFrame = Time.frameCount + RetryIntervalFrames;
                    _behaviour.Object.ReleaseStateAuthority();
                }

                return;
            }

            var eligible = _canClaim?.Invoke() ?? _behaviour.Runner.IsLocalPlayerSessionOwner();
            if (!eligible)
                return;

            _nextRequestFrame = Time.frameCount + RetryIntervalFrames;
            _behaviour.Object.RequestStateAuthority();
        }

        private bool HasAuthority() =>
            _behaviour && _behaviour.Object && _behaviour.Object.HasStateAuthority;

        public void Dispose()
        {
            _started = false;
        }
    }
}
