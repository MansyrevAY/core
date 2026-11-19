using System;

namespace Core.Networking.Core
{
    public readonly struct RunnerGeneration : IEquatable<RunnerGeneration>
    {
        public long Value { get; }

        public RunnerGeneration(long value)
        {
            Value = value;
        }

        public bool Equals(RunnerGeneration other) => Value == other.Value;
        public override bool Equals(object value) => value is RunnerGeneration other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(RunnerGeneration left, RunnerGeneration right) => left.Equals(right);
        public static bool operator !=(RunnerGeneration left, RunnerGeneration right) => !left.Equals(right);
    }

    public readonly struct OperationId : IEquatable<OperationId>
    {
        public long Value { get; }

        public OperationId(long value)
        {
            Value = value;
        }

        public bool Equals(OperationId other) => Value == other.Value;
        public override bool Equals(object value) => value is OperationId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(OperationId left, OperationId right) => left.Equals(right);
        public static bool operator !=(OperationId left, OperationId right) => !left.Equals(right);
    }

    public enum OperationStatus
    {
        Success,
        Failed,
        Cancelled,
        Busy,
        Rejected
    }

    public enum ConnectionPhase
    {
        Disconnected,
        ConnectingLobby,
        Lobby,
        JoiningRoom,
        InRoom,
        Disconnecting,
        Faulted
    }

    public enum LeaveMode
    {
        KeepReconnect,
        ForgetSession
    }

    public enum NetworkErrorStage
    {
        Admission,
        Regions,
        Shutdown,
        Lobby,
        Join,
        Scene,
        LocalPlayer,
        Barrier,
        Storage
    }

    public enum NetworkErrorCode
    {
        InvalidState,
        Disposed,
        Timeout,
        Transport,
        ShutdownFailed,
        InvalidRequest,
        Unexpected
    }

    public sealed class NetworkError
    {
        public NetworkErrorStage Stage { get; }
        public NetworkErrorCode Code { get; }
        public string Reason { get; }

        public NetworkError(NetworkErrorStage stage, NetworkErrorCode code, string reason)
        {
            Stage = stage;
            Code = code;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct OperationResult
    {
        public OperationStatus Status { get; }
        public OperationId OperationId { get; }
        public RunnerGeneration Generation { get; }
        public NetworkError Error { get; }
        public bool IsSuccess => Status == OperationStatus.Success;

        public OperationResult(OperationStatus status, OperationId operationId, RunnerGeneration generation,
            NetworkError error = null)
        {
            Status = status;
            OperationId = operationId;
            Generation = generation;
            Error = error;
        }

        public void ThrowIfNotSuccess()
        {
            if (IsSuccess)
            {
                return;
            }

            if (Status == OperationStatus.Cancelled)
            {
                throw new OperationCanceledException("Network operation cancelled");
            }

            throw new NetworkOperationException(this);
        }
    }

    public sealed class NetworkOperationException : Exception
    {
        public OperationResult Result { get; }
        public override string Message
        {
            get;
        }

        public NetworkOperationException(OperationResult result)
        {
            Result = result;
            Message = FormatMessage(result);
        }

        private static string FormatMessage(OperationResult result)
        {
            var reason = result.Error?.Reason;
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = result.Status switch
                {
                    OperationStatus.Busy => "Another network operation is already in progress",
                    OperationStatus.Rejected => "Network operation was rejected",
                    _ => "Network operation failed"
                };
            }

            var details = result.Error == null ? "" : $", {result.Error.Stage}/{result.Error.Code}";
            return $"{reason} (operation {result.OperationId}: {result.Status}{details})";
        }
    }

    public sealed class SessionLocator
    {
        public string Name { get; }
        public string Region { get; }
        public bool IsComplete => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Region);

        public SessionLocator(string name, string region)
        {
            Name = name ?? string.Empty;
            Region = region ?? string.Empty;
        }
    }

    public sealed class CreateSessionRequest
    {
        public string Name { get; }
        public bool IsVisible { get; }
        public bool SaveSession { get; }
        public bool IsSolo { get; }
        public int? PlayerCount { get; }

        public CreateSessionRequest(string name, bool isVisible = true, bool saveSession = true, bool isSolo = false,
            int? playerCount = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? Guid.NewGuid().ToString("N") : name;
            IsVisible = isVisible;
            SaveSession = saveSession;
            IsSolo = isSolo;
            PlayerCount = playerCount;
        }
    }
}
