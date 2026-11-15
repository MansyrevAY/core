using System;
using Core.Infra.Logging;
using Cysharp.Threading.Tasks;

namespace Core.Infra.Utils
{
    public static class TaskUtils
    {
        // Runs a task without awaiting it, logging instead of losing its failure.
        // Named differently from UniTask's own Forget on purpose, so the two never become ambiguous.
        public static async void FireAndForget(this UniTask task)
        {
            try
            {
                await task;
            }
            // UniTask cancels with OperationCanceledException; TaskCanceledException derives from it,
            // so this also covers cancellation coming from System.Threading.Tasks.
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
