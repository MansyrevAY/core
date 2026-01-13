using System;
using Cysharp.Threading.Tasks;

namespace Core.Infra.Command
{
    public class SimpleStep : IStep
    {
        private readonly Action _action;
        
        public SimpleStep(Action action)
        {
            _action = action;
        }

        public UniTask ExecuteAsync()
        {
            _action.Invoke();
            
            return UniTask.CompletedTask;
        }
    }
}