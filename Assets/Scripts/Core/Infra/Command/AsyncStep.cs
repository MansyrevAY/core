using System;
using Cysharp.Threading.Tasks;

namespace Core.Infra.Command
{
    public class AsyncStep : IStep
    {
        private readonly Func<UniTask> _func;
        
        public AsyncStep(Func<UniTask> func)
        {
            _func = func;
        }
        
        public UniTask ExecuteAsync()
        {
            return _func.Invoke();
        }
    }
}