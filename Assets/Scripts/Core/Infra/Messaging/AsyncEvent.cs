using System.Threading.Tasks;

namespace Core.Infra.Messaging
{
    public abstract class AsyncEvent
    {
        public readonly TaskCompletionSource<bool> Source;
        
        protected AsyncEvent()
        {
            Source = new TaskCompletionSource<bool>();
        }
    }
}