using System;
using Core.Infra.Logging;
using Cysharp.Threading.Tasks;

namespace Core.Infra.Utils
{
    public static class TaskUtils
    {
        public static async void FireAndForget(this UniTask task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException e)
            {
                Log.Warning($"Task cancelled: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
