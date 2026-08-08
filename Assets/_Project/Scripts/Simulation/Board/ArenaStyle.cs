namespace BomberLegends.Simulation.Board
{
    /// <summary>The shape of permanent structure an arena is built from.</summary>
    /// <remarks>
    /// Style is what makes two generated arenas feel different rather than merely re-rolled. Density
    /// alone produces the same room with more clutter in it.
    /// </remarks>
    public enum ArenaStyle : byte
    {
        /// <summary>Classic Bomberman: a pillar on every second tile. Open, and always connected.</summary>
        Lattice = 0,

        /// <summary>Loose pillars with wide gaps. Reads as open ground with cover.</summary>
        Scattered = 1,

        /// <summary>Long walls with doorways, making rooms and sightlines.</summary>
        Chambers = 2
    }
}
