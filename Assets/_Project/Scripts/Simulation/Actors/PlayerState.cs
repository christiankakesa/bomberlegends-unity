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

        /// <summary>How many bombs the player may have on the board at once.</summary>
        public int BombCapacity;

        /// <summary>How many of theirs are currently ticking.</summary>
        public int ActiveBombs;

        /// <summary>How many tiles each arm of their blast reaches.</summary>
        public int BlastRange;

        /// <summary>Ticks before another bomb may be placed. Zero under the classic capacity model.</summary>
        public int BombCooldownTicksRemaining;

        /// <summary>
        /// Whether the bomb button was already down last tick, so placement triggers on the press
        /// rather than draining the whole pool while the button is held.
        /// </summary>
        public bool BombHeldLastTick;

        /// <summary>The tile the player occupies.</summary>
        public readonly GridCoord Tile => Position.Tile;

        /// <summary>Creates a player standing at the centre of a tile.</summary>
        public static PlayerState SpawnedAt(GridCoord tile, int bombCapacity, int blastRange) =>
            new PlayerState
            {
                Position = SubTilePoint.AtCentreOf(tile),
                MoveDirection = Direction.None,
                Facing = Direction.South,
                IsMoving = false,
                BombCapacity = bombCapacity,
                ActiveBombs = 0,
                BlastRange = blastRange,
                BombCooldownTicksRemaining = 0,
                BombHeldLastTick = false
            };
    }
}
