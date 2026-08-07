using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Actors
{
    /// <summary>
    /// Every enemy that can exist at once, in a fixed number of slots.
    /// </summary>
    /// <remarks>
    /// Slot-based like the bomb pool: an enemy keeps its index for its whole life, so events can name
    /// one without anything being invalidated when another dies mid-resolution.
    /// </remarks>
    public struct EnemyBuffer
    {
        private readonly EnemyState[] _enemies;

        /// <summary>Creates a buffer with the given number of slots.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The capacity is not positive.</exception>
        public EnemyBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "Enemy capacity must be positive.");
            }

            _enemies = new EnemyState[capacity];
        }

        /// <summary>How many slots exist.</summary>
        public readonly int Capacity => _enemies.Length;

        /// <summary>Reads or writes a slot.</summary>
        public EnemyState this[int index]
        {
            readonly get => _enemies[index];
            set => _enemies[index] = value;
        }

        /// <summary>How many enemies are still alive.</summary>
        public readonly int AliveCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _enemies.Length; i++)
                {
                    if (_enemies[i].IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Claims a slot, or returns <c>-1</c> when every slot is in use.</summary>
        public int Spawn(GridCoord tile, int maxHealth)
        {
            for (var i = 0; i < _enemies.Length; i++)
            {
                if (_enemies[i].IsActive)
                {
                    continue;
                }

                _enemies[i] = EnemyState.SpawnedAt(tile, maxHealth);
                return i;
            }

            return -1;
        }

        /// <summary>Frees a slot.</summary>
        public void Remove(int index) => _enemies[index].IsActive = false;
    }
}
