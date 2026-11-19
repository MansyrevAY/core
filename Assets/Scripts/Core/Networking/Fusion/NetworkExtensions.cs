using Core.Resources;
using Fusion;

namespace Core.Networking.Fusion
{
    public static class Extensions
    {
        public static T OrNull<T>(this T obj) where T : UnityEngine.Object
        {
            return obj != null ? obj : null;
        }

        public static bool IsAlive<T>(this T obj) where T : class
        {
            if (obj is UnityEngine.Object unityObject)
            {
                return unityObject;
            }

            return obj != null;
        }

        public static bool IsLocalPlayerSessionOwner(this NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning) return false;
            var lowestPlayer = PlayerRef.None;
            foreach (var player in runner.ActivePlayers)
            {
                if (lowestPlayer == PlayerRef.None || player.PlayerId < lowestPlayer.PlayerId)
                    lowestPlayer = player;
            }
            return lowestPlayer != PlayerRef.None && runner.LocalPlayer == lowestPlayer;
        }

        public static long StableConnectionTokenId(byte[] token)
        {
            if (token == null || token.Length == 0)
                return 0;

            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (var value in token)
                {
                    hash ^= value;
                    hash *= 1099511628211UL;
                }

                return hash == 0 ? 1 : (long)hash;
            }
        }
    }
}
