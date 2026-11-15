using Core.Infra.Client;

namespace Core.Infra.Module
{
    public interface IModule
    {
        void Init(IClient client);
        void Cleanup(IClient client);
    }
}