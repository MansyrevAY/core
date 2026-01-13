namespace Core.Infra.Module
{
    public interface IInternalModuleProvider
    {
        T GetModule<T>() where T: class, IInternalModule;
    }
}