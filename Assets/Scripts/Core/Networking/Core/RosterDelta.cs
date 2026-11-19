using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Core.Networking.Core
{
    public sealed class RosterDelta<T>
    {
        public ReadOnlyCollection<T> Joined { get; }
        public ReadOnlyCollection<T> Left { get; }

        public RosterDelta(IEnumerable<T> previous, IEnumerable<T> current)
        {
            var before = new HashSet<T>(previous);
            var after = new HashSet<T>(current);
            var joined = new List<T>();
            var left = new List<T>();
            foreach (var value in after)
            {
                if (!before.Contains(value))
                {
                    joined.Add(value);
                }
            }

            foreach (var value in before)
            {
                if (!after.Contains(value))
                {
                    left.Add(value);
                }
            }

            Joined = joined.AsReadOnly();
            Left = left.AsReadOnly();
        }
    }
}
