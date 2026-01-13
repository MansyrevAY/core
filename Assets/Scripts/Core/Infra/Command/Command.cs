using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Core.Infra.Command
{
    public class Command
    {
        private readonly List<IStep> _steps = new();
        
        public void Execute()
        {
            ExecuteAsync().Forget();
        }

        public async UniTask ExecuteAsync()
        {
            foreach (var step in _steps)
            {
                await step.ExecuteAsync();
            }
        }

        protected void Add(Action action)
        {
            _steps.Add(new SimpleStep(action));
        }

        protected void Add(Func<UniTask> action)
        {
            _steps.Add(new AsyncStep(action));
        }
    }
}
