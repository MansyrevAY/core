using Core.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Core.Networking.Core;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using R3;
using UnityEngine;
using System.Threading;

namespace Core.Networking.Fusion
{
    public sealed class LobbyDirectory : IDisposable
    {
        public const string ConfirmedPlayersPropertyKey = "confirmedPlayers";
        private readonly ReactiveProperty<ReadOnlyCollection<SessionData>> _sessions = CreateInitialSessions();

        private static ReactiveProperty<ReadOnlyCollection<SessionData>> CreateInitialSessions()
        {
            var sessions = Array.Empty<SessionData>();
            var readOnlySessions = Array.AsReadOnly(sessions);
            return new ReactiveProperty<ReadOnlyCollection<SessionData>>(readOnlySessions);
        }

        private RegionInfo[] _regions = Array.Empty<RegionInfo>();
        public ReadOnlyReactiveProperty<ReadOnlyCollection<SessionData>> Sessions => _sessions;
        public RunnerGeneration Generation { get; private set; }
        public string Region { get; private set; }

        internal void Reset(RunnerGeneration generation)
        {
            Generation = generation;
            Region = null;
            var array = Array.Empty<SessionData>();
            _sessions.Value = Array.AsReadOnly(array);
        }

        internal void Update(RunnerGeneration generation, string region, IEnumerable<SessionInfo> sessions)
        {
            if (generation != Generation)
            {
                return;
            }

            Region = region;
            var array = sessions.Where(session => session.IsValid).Select(session =>
            {
                var name = session.Name;
                var playerCount =
                    session.Properties.TryGetValue(ConfirmedPlayersPropertyKey, out var confirmed) && confirmed.IsInt
                        ? (int)confirmed
                        : session.PlayerCount;
                var maxPlayers = session.MaxPlayers;
                return new SessionData(name, playerCount, maxPlayers, region, session.IsOpen, session.IsVisible);
            }).ToArray();
            _sessions.Value = Array.AsReadOnly(array);
        }

        internal async UniTask FetchRegionsAsync(CancellationToken token)
        {
            try
            {
                _regions = (await NetworkRunner.GetAvailableRegions(cancellationToken: token).AsUniTask()).ToArray();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[LobbyDirectory] Failed to fetch regions: {error.Message}");
            }
        }

        public int GetRegionPing(string region)
        {
            if (string.IsNullOrEmpty(region))
            {
                return -1;
            }

            foreach (var item in _regions)
            {
                if (item.RegionCode == region)
                {
                    return item.RegionPing;
                }
            }

            return -1;
        }

        internal string BestRegion =>
            _regions.Length == 0 ? "" : _regions.OrderBy(region => region.RegionPing).First().RegionCode;

        public void Dispose() => _sessions.Dispose();
    }
}
