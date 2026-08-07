using BomberLegends.Core;

namespace BomberLegends.Simulation.Skills
{
    /// <summary>One skillshot in flight.</summary>
    public struct ProjectileState
    {
        /// <summary>Exact position in sub-tile units.</summary>
        public SubTilePoint Position;

        /// <summary>Travel per tick along each axis, in sub-tile units.</summary>
        public int VelocityX;

        /// <summary>Travel per tick along each axis, in sub-tile units.</summary>
        public int VelocityY;

        /// <summary>Ticks left before it expires.</summary>
        /// <remarks>
        /// Lifetime is counted in ticks rather than distance so range falls out of speed times
        /// duration exactly, with no accumulated rounding — and so an item that raises speed
        /// lengthens the shot as well, which is how a player expects it to behave.
        /// </remarks>
        public int TicksRemaining;

        /// <summary>Damage it deals on contact.</summary>
        public int Damage;

        /// <summary>Behaviours carried over from the skill that fired it.</summary>
        public SkillTraits Traits;

        /// <summary>
        /// The tile it was fired from.
        /// </summary>
        /// <remarks>
        /// Kept so a detonating shot cannot set off the bomb the player is standing on. Without
        /// that exemption, equipping such an item would turn every shot fired while stood over your
        /// own bomb into a suicide — and it would quietly contradict the "walk off your own bomb"
        /// grace the game already grants.
        /// </remarks>
        public GridCoord OriginTile;

        /// <summary>Whether this slot holds a projectile in flight.</summary>
        public bool IsActive;

        /// <summary>The tile it currently occupies.</summary>
        public readonly GridCoord Tile => Position.Tile;
    }
}
