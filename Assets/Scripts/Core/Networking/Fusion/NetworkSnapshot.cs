using Core.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Core.Networking.Core;
using Fusion;

namespace Core.Networking.Fusion {
    public sealed class NetworkSnapshot {
        public RunnerGeneration Generation { get; }
        public ConnectionPhase Phase { get; }
        public SessionLocator Session { get; }
        public PlayerRef LocalPlayer { get; }
        public bool IsOwner { get; }
        public ReadOnlyCollection<PlayerRef> Peers { get; }

        public NetworkSnapshot(RunnerGeneration generation, ConnectionPhase phase, SessionLocator session = null,
            PlayerRef localPlayer = default, bool isOwner = false, IEnumerable<PlayerRef> peers = null)
        {
            Generation = generation;
            Phase = phase;
            Session = session;
            LocalPlayer = localPlayer;
            IsOwner = isOwner;
            var array = peers == null ? Array.Empty<PlayerRef>() : new List<PlayerRef>(peers).ToArray();
            Peers = Array.AsReadOnly(array);
        }
    }
}
