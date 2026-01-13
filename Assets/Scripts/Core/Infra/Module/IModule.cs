using Core.Infra.Client;

namespace Core.Infra.Module
{
    public interface IModule
    {
        void Init(IClient client);
    }
}