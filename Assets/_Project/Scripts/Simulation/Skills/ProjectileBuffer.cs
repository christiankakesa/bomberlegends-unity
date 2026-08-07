using System;

namespace BomberLegends.Simulation.Skills
{
    /// <summary>
    /// Every skillshot that can be in flight at once, in a fixed number of slots.
    /// </summary>
    /// <remarks>
    /// Slot-based like bombs and enemies, so an event can name a projectile without anything being
    /// invalidated when another expires in the same tick.
    /// </remarks>
    public struct ProjectileBuffer
    {
        private readonly ProjectileState[] _projectiles;

        /// <summary>Creates a buffer with the given number of slots.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The capacity is not positive.</exception>
        public ProjectileBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "Projectile capacity must be positive.");
            }

            _projectiles = new ProjectileState[capacity];
        }

        /// <summary>How many slots exist.</summary>
        public readonly int Capacity => _projectiles.Length;

        /// <summary>Reads or writes a slot.</summary>
        public ProjectileState this[int index]
        {
            readonly get => _projectiles[index];
            set => _projectiles[index] = value;
        }

        /// <summary>How many are currently in flight.</summary>
        public readonly int ActiveCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _projectiles.Length; i++)
                {
                    if (_projectiles[i].IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Claims a slot, or returns <c>-1</c> when every slot is in use.</summary>
        public int Fire(
            Core.SubTilePoint origin,
            int velocityX,
            int velocityY,
            int lifetimeTicks,
            int damage,
            SkillTraits traits = SkillTraits.None)
        {
            for (var i = 0; i < _projectiles.Length; i++)
            {
                if (_projectiles[i].IsActive)
                {
                    continue;
                }

                _projectiles[i] = new ProjectileState
                {
                    Position = origin,
                    VelocityX = velocityX,
                    VelocityY = velocityY,
                    TicksRemaining = lifetimeTicks,
                    Damage = damage,
                    Traits = traits,
                    OriginTile = origin.Tile,
                    IsActive = true
                };

                return i;
            }

            return -1;
        }

        /// <summary>Frees a slot.</summary>
        public void Remove(int index) => _projectiles[index].IsActive = false;
    }
}
