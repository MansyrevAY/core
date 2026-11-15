using Cysharp.Threading.Tasks;

namespace Core.Infra.Commands
{
    public interface IStep
    {
        UniTask ExecuteAsync();
    }
}