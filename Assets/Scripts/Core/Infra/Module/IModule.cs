namespace Core.Infra.Module
{
    public interface IModule
    {
        // Side effects only (subscribe, create entities, find scene objects). Dependencies arrive via the constructor.
        void Start();
        void Cleanup();
    }
}
