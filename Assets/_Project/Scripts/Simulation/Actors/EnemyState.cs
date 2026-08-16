using BomberLegends.Core;

namespace BomberLegends.Simulation.Actors
{
    /// <summary>One enemy in the arena.</summary>
    public struct EnemyState
    {
        /// <summary>Exact position in sub-tile units.</summary>
        public SubTilePoint Position;

        /// <summary>Health and immunity.</summary>
        public HealthState Health;

        /// <summary>The way it is currently travelling.</summary>
        public Direction MoveDirection;

        /// <summary>
        /// A heading that just failed to move it, so it is not chosen again immediately.
        /// </summary>
        /// <remarks>
        /// The chase picks directions by tile but travels as a box, and those two can disagree: a
        /// tile ahead reads walkable while the box is clipping the corner of the pillar beside it.
        /// Without this memory the enemy re-picks the same blocked heading every tick and never
        /// leaves. Lane centring keeps that from arising; this makes sure it cannot persist.
        /// </remarks>
        public Direction BlockedDirection;

        /// <summary>
        /// Whether this Sentinel has noticed the player.
        /// </summary>
        /// <remarks>
        /// Dormant until the player comes close, then awake for good. Without it every enemy in the
        /// arena converges from the first tick, which is what made the second sector unplayable in
        /// testing: five pursuers arriving together is not five encounters, it is one that cannot be
        /// fought. Waking never reverses, because an enemy that loses interest at a threshold
        /// oscillates on and off at exactly the distance a player is most likely to be standing.
        /// </remarks>
        public bool IsAlerted;

        /// <summary>Whether this slot holds a live enemy.</summary>
        public bool IsActive;

        /// <summary>The tile it occupies.</summary>
        public readonly GridCoord Tile => Position.Tile;

        /// <summary>Creates an enemy standing at the centre of a tile.</summary>
        public static EnemyState SpawnedAt(GridCoord tile, int maxHealth) => new EnemyState
        {
            Position = SubTilePoint.AtCentreOf(tile),
            Health = HealthState.Full(maxHealth),
            MoveDirection = Direction.None,
            BlockedDirection = Direction.None,
            IsAlerted = false,
            IsActive = true
        };
    }
}
