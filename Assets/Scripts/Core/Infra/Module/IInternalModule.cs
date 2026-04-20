using Core.Infra.Broadcaster;

namespace Core.Infra.Module
{
    public interface IInternalModule
    {
        IBroadcaster Broadcaster { get; }
    }
}