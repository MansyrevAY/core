namespace Core.Infra.Module
{
    public interface IModuleProvider
    {
        T GetModule<T>() where T: class, IModule;
    }
}