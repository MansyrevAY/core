using Core.Infra.Bootstrap;
using Core.Infra.Logging;

namespace Game
{
    public class GameClient : Client
    {
        protected override void CreateModules()
        {
            base.CreateModules();
        }

        protected override void StartGame()
        {
            base.StartGame();

            Log.Info("Game started");
        }
    }
}
