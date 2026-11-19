using Core.Resources;
using Core.Networking.Core;
using Fusion;

namespace Core.Networking.Fusion
{
    public enum NetworkEventKind { PhaseChanged, OwnerChanged, ParticipantJoined, ParticipantLeft }

    public readonly struct NetworkEvent
    {
        public NetworkEventKind Kind { get; }
        public RunnerGeneration Generation => Snapshot.Generation;
        public OperationId OperationId { get; }
        public PlayerRef Player { get; }
        public NetworkSnapshot Snapshot { get; }

        public NetworkEvent(NetworkEventKind kind, NetworkSnapshot snapshot, OperationId operationId,
            PlayerRef player = default)
        {
            Kind = kind;
            Snapshot = snapshot;
            OperationId = operationId;
            Player = player;
        }
    }
}
