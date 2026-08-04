using System;

namespace BomberLegends.Simulation.Events
{
    /// <summary>
    /// The events produced by a single tick.
    /// </summary>
    /// <remarks>
    /// Fixed capacity, allocated once. The view drains it every frame. If a tick ever produces more
    /// events than it holds, the overflow is dropped and counted rather than growing the buffer:
    /// a silent allocation mid-match is worse than a dropped effect, and the count makes the
    /// undersizing visible instead of mysterious.
    /// </remarks>
    public sealed class SimEventBuffer
    {
        private readonly SimEvent[] _events;

        /// <summary>Creates a buffer.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The capacity is not positive.</exception>
        public SimEventBuffer(int capacity = 256)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "Event capacity must be positive.");
            }

            _events = new SimEvent[capacity];
        }

        /// <summary>How many events the current tick produced.</summary>
        public int Count { get; private set; }

        /// <summary>How many events have been dropped because the buffer was full.</summary>
        public int DroppedCount { get; private set; }

        /// <summary>The most events the buffer can hold in one tick.</summary>
        public int Capacity => _events.Length;

        /// <summary>Reads an event from the current tick.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is outside <see cref="Count"/>.</exception>
        public SimEvent this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(index), index, $"Only {Count} events were produced this tick.");
                }

                return _events[index];
            }
        }

        /// <summary>Records an event, or counts a drop if the buffer is full.</summary>
        public void Add(in SimEvent simEvent)
        {
            if (Count >= _events.Length)
            {
                DroppedCount++;
                return;
            }

            _events[Count++] = simEvent;
        }

        /// <summary>Empties the buffer, ready for the next tick.</summary>
        public void Clear() => Count = 0;

        /// <summary>Resets the dropped counter, after it has been reported.</summary>
        public void ResetDroppedCount() => DroppedCount = 0;
    }
}
