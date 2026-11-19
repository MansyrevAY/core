using Core.Networking.Core;
using Fusion;
using UnityEngine;

namespace Core.Networking.Players
{
    public sealed class PlayerSessionContext : MonoBehaviour
    {
        public PlayerService Players { get; private set; }
        public LocalPlayerBootstrapper Bootstrap { get; private set; }
        public PlayerSpawnCoordinator Spawning { get; private set; }
        public PlayerSessionDirector Director { get; set; }
        public RunnerGeneration Generation { get; private set; }

        public void Configure(PlayerService players, RunnerGeneration generation)
        {
            Players = players;
            Generation = generation;
            Bootstrap = new LocalPlayerBootstrapper();
            Spawning = new PlayerSpawnCoordinator();
        }
    }
}
