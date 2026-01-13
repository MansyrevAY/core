using Core.Infra.Service;

namespace Core.Services.Data
{
    public interface IRecordService : IService
    {
        void CreateAll();
        T GetRecord<T>() where T : BaseRecord;
    }
}