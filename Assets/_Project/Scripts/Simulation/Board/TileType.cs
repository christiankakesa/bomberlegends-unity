namespace BomberLegends.Simulation.Board
{
    /// <summary>What occupies a tile of the board.</summary>
    public enum TileType : byte
    {
        /// <summary>Free floor. Actors and blasts pass through.</summary>
        Empty = 0,

        /// <summary>Permanent structure. Stops movement and stops a blast.</summary>
        Solid = 1,

        /// <summary>
        /// Destructible block. Stops movement and stops a blast, and is removed by the blast that
        /// reaches it.
        /// </summary>
        Destructible = 2
    }
}
