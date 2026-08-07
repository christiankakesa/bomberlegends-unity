namespace BomberLegends.Input
{
    /// <summary>
    /// Supplies the direction the player is aiming, in grid space.
    /// </summary>
    /// <remarks>
    /// Declared here and implemented by Gameplay, the same inversion as
    /// <see cref="IGridProjection"/>: pointing at a spot on the ground needs the camera and the
    /// player's world position, neither of which Input may reference. Returning a direction rather
    /// than a point keeps the simulation's input surface to the two bytes it already reserves.
    /// </remarks>
    public interface IAimSource
    {
        /// <summary>
        /// Reads the current aim as a grid-space direction, which need not be normalised.
        /// </summary>
        /// <returns><see langword="false"/> when the player is not aiming at anything.</returns>
        bool TryGetAim(out float gridX, out float gridY);
    }
}
