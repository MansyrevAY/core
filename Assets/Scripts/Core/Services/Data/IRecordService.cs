using Core.Infra.Service;
using Cysharp.Threading.Tasks;

namespace Core.Services.Data
{
    public interface IRecordService : IService
    {
        UniTask CreateAll();
        T GetRecord<T>() where T : BaseRecord;
    }
}