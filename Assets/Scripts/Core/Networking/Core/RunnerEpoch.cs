using System;

namespace Core.Networking.Core {
    public sealed class RunnerEpoch<T> where T : class {
        public RunnerGeneration Generation { get; private set; }
        public T Current { get; private set; }

        public RunnerGeneration Activate(T instance) {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (Current != null)
            {
                throw new InvalidOperationException("Previous runner has not been retired");
            }

            Generation = new RunnerGeneration(checked(Generation.Value + 1));
            Current = instance;
            return Generation;
        }

        public bool IsCurrent(T instance, RunnerGeneration generation) =>
            instance != null && ReferenceEquals(Current, instance) && Generation == generation;

        public bool Retire(T instance, RunnerGeneration generation) {
            if (!IsCurrent(instance, generation))
            {
                return false;
            }

            Current = null;
            return true;
        }
    }
}
