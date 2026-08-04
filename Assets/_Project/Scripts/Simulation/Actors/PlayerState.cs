using BomberLegends.Core;

namespace BomberLegends.Simulation.Actors
{
    /// <summary>The player's position and heading.</summary>
    /// <remarks>
    /// Position is continuous in sub-tile units while the tile it resolves to drives every rule —
    /// the "soft grid" the design calls for: movement reads as smooth, but bomb placement,
    /// occupancy and blast damage all snap to whole tiles.
    /// </remarks>
    public struct PlayerState
    {
        /// <summary>Exact position in sub-tile units.</summary>
        public SubTilePoint Position;

        /// <summary>The direction the player is travelling, or none when stationary.</summary>
        public Direction MoveDirection;

        /// <summary>
        /// The direction the player is looking. Keeps its value when the player stops, so a
        /// stationary character does not snap back to a default pose.
        /// </summary>
        public Direction Facing;

        /// <summary>Whether the player actually moved on the last tick.</summary>
        public bool IsMoving;

        /// <summary>The tile the player occupies.</summary>
        public readonly GridCoord Tile => Position.Tile;

        /// <summary>Creates a player standing at the centre of a tile.</summary>
        public static PlayerState SpawnedAt(GridCoord tile) => new PlayerState
        {
            Position = SubTilePoint.AtCentreOf(tile),
            MoveDirection = Direction.None,
            Facing = Direction.South,
            IsMoving = false
        };
    }
}
