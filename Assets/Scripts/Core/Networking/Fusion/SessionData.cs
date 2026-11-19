using Core.Resources;
namespace Core.Networking.Fusion
{
    public sealed class SessionData
    {
        public string Name { get; }
        public int PlayerCount { get; }
        public int MaxPlayers { get; }
        public string Region { get; }
        public bool IsOpen { get; }
        public bool IsVisible { get; }
        public SessionData(string name, int playerCount, int maxPlayers, string region, bool isOpen, bool isVisible)
        {
            Name = name; PlayerCount = playerCount; MaxPlayers = maxPlayers;
            Region = region; IsOpen = isOpen; IsVisible = isVisible;
        }
    }
}
