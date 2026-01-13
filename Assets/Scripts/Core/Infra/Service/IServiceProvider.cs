namespace Core.Infra.Service
{
    public interface IServiceProvider
    {
        T GetService<T>() where T : class, IService;
    }
}