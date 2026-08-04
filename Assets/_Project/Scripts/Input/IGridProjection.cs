using UnityEngine;

namespace BomberLegends.Input
{
    /// <summary>
    /// Converts a screen-space direction into grid space.
    /// </summary>
    /// <remarks>
    /// Declared here, by the layer that needs it, and implemented by the renderer that owns the
    /// projection. Input cannot reference Gameplay — the dependency runs the other way — and
    /// duplicating the projection maths on both sides would let the controls and the picture drift
    /// apart without anything failing.
    /// </remarks>
    public interface IGridProjection
    {
        /// <summary>Converts a screen-space direction into grid space.</summary>
        Vector2 ScreenToGrid(Vector2 screenDirection);
    }
}
