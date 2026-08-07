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
            IsActive = true
        };
    }
}
