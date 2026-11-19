using System;
using System.Collections.Generic;

namespace Core.Networking.Core
{
    public enum SceneRegistrationStatus
    {
        Waiting,
        Registered,
        Failed,
        Cancelled
    }

    public sealed class SceneRegistrationOperation<TScene>
    {
        public OperationId Id { get; }
        public RunnerGeneration Generation { get; }
        public TScene Target { get; }
        public float Progress { get; private set; }
        public SceneRegistrationStatus Status { get; private set; }
        public bool IsWaiting => Status == SceneRegistrationStatus.Waiting;

        public SceneRegistrationOperation(OperationId id, RunnerGeneration generation, TScene target)
        {
            Id = id;
            Generation = generation;
            Target = target;
        }

        private bool Accept(RunnerGeneration generation, TScene scene) =>
            IsWaiting && generation == Generation && EqualityComparer<TScene>.Default.Equals(Target, scene);

        public bool ObserveProgress(RunnerGeneration generation, TScene scene, float progress)
        {
            if (IsProgressObservationInvalid(generation, scene, progress))
            {
                return false;
            }

            var previousProgress = Progress;
            var cappedProgress = Math.Min(1f, progress);
            var clampedProgress = Math.Max(0f, cappedProgress);
            Progress = Math.Max(previousProgress, clampedProgress);
            return true;
        }

        public bool ObserveRegistration(RunnerGeneration generation, TScene scene)
        {
            if (!Accept(generation, scene))
            {
                return false;
            }

            Progress = 1f;
            Status = SceneRegistrationStatus.Registered;
            return true;
        }

        public void Stop(bool failed)
        {
            if (IsWaiting)
            {
                Status = failed ? SceneRegistrationStatus.Failed : SceneRegistrationStatus.Cancelled;
            }
        }

        private bool IsProgressObservationInvalid(RunnerGeneration generation, TScene scene, float progress)
        {
            return !Accept(generation, scene) || float.IsNaN(progress) || float.IsInfinity(progress);
        }
    }
}
