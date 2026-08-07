using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Bombs
{
    /// <summary>
    /// Every bomb that can exist at once, in a fixed number of slots.
    /// </summary>
    /// <remarks>
    /// Slot-based rather than a compacting list: a bomb keeps the same index for its whole life, so
    /// the occupancy grid can store an index and the chain-detonation queue can reference bombs
    /// without anything being invalidated mid-resolution. Allocated once and never resized.
    /// </remarks>
    public struct BombBuffer
    {
        private readonly BombState[] _bombs;

        /// <summary>Creates a buffer with the given number of slots.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The capacity is not positive.</exception>
        public BombBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "Bomb capacity must be positive.");
            }

            _bombs = new BombState[capacity];
        }

        /// <summary>How many slots exist.</summary>
        public readonly int Capacity => _bombs.Length;

        /// <summary>Reads or writes a slot.</summary>
        public BombState this[int index]
        {
            readonly get => _bombs[index];
            set => _bombs[index] = value;
        }

        /// <summary>How many slots currently hold a live bomb.</summary>
        public readonly int ActiveCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _bombs.Length; i++)
                {
                    if (_bombs[i].IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Claims a free slot for a bomb, or returns <c>-1</c> when every slot is in use.
        /// </summary>
        public int Place(GridCoord tile, int fuseTicks, int range)
        {
            for (var i = 0; i < _bombs.Length; i++)
            {
                if (_bombs[i].IsActive)
                {
                    continue;
                }

                _bombs[i] = new BombState
                {
                    Tile = tile,
                    FuseTicksRemaining = fuseTicks,
                    Range = range,
                    IsActive = true,
                    IsQueued = false
                };

                return i;
            }

            return -1;
        }

        /// <summary>Frees a slot.</summary>
        public void Remove(int index)
        {
            _bombs[index].IsActive = false;
            _bombs[index].IsQueued = false;
        }

        /// <summary>Clears every slot.</summary>
        public void Clear()
        {
            for (var i = 0; i < _bombs.Length; i++)
            {
                _bombs[i].IsActive = false;
                _bombs[i].IsQueued = false;
            }
        }
    }
}
