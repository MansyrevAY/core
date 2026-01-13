using System.Threading.Tasks;

namespace Core.Infra.Broadcaster
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