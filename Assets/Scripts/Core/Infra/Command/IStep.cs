using Cysharp.Threading.Tasks;

namespace Core.Infra.Command
{
    public interface IStep
    {
        UniTask ExecuteAsync();
    }
}