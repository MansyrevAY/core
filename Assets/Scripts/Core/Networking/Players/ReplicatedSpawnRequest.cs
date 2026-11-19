using Core.Networking.Core;
using Fusion;

namespace Core.Networking.Players
{
    public struct ReplicatedSpawnRequest : INetworkStruct
    {
        public NetworkString<_32> Id;
        public PlayerRef Player;
        public long Identity;
        public SpawnRequestStage Stage;
        public SpawnOutcome Outcome;
        public SpawnFailure Failure;
        public NetworkId Avatar;
        public int Epoch;
        public NetworkBool Reserved;
        public TickTimer Deadline;

        public SpawnAdmissionRecord Read()
        {
            var idText = Id.ToString();
            var spawnRequest = new SpawnRequest(idText, Player.RawEncoded, Identity);
            return new(spawnRequest, Stage, Outcome, Failure, Avatar.Raw, Epoch, Reserved);
        }

        public static ReplicatedSpawnRequest From(SpawnAdmissionRecord record) => new()
        {
            Id = record.Request.Id, Player = PlayerRef.FromEncoded(record.Request.Player),
            Identity = record.Request.Identity,
            Stage = record.Stage, Outcome = record.Outcome, Failure = record.Failure,
            Avatar = new NetworkId { Raw = record.AvatarId },
            Epoch = record.AuthorityEpoch, Reserved = record.HoldsReservation
        };
    }

}
