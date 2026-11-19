using Core.Infra.Bootstrap;
using Game.Demo;

namespace Game
{
    public class GameClient : Client
    {
        protected override void CreateModules()
        {
            base.CreateModules();
            AddModule(new CoopDemoModule());
        }
    }
}
