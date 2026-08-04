using BomberLegends.Core;
using UnityEngine;

namespace BomberLegends.Input
{
    /// <summary>
    /// Turns an analogue stick into one of four grid directions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pure functions with no Unity state so the behaviour that decides how the game feels can be
    /// unit tested rather than only felt.
    /// </para>
    /// <para>
    /// The hysteresis is the important part. Snapping to whichever axis is larger makes a thumb
    /// resting near a diagonal flicker between two directions several times a second, which reads as
    /// the character stuttering. Requiring a new axis to beat the current one by a margin removes
    /// that entirely, at the cost of turns needing slightly more deliberate thumb movement.
    /// </para>
    /// </remarks>
    public static class DirectionSnapper
    {
        /// <summary>
        /// Snaps a grid-space stick vector to a cardinal direction, preferring to stay on
        /// <paramref name="current"/>.
        /// </summary>
        /// <param name="gridDirection">Stick vector already converted to grid space.</param>
        /// <param name="current">The direction currently being travelled.</param>
        /// <param name="switchRatio">
        /// How much larger a perpendicular axis must be before the direction changes. One means no
        /// hysteresis; larger values make direction changes progressively more deliberate.
        /// </param>
        public static Direction Snap(Vector2 gridDirection, Direction current, float switchRatio)
        {
            var absX = Mathf.Abs(gridDirection.x);
            var absY = Mathf.Abs(gridDirection.y);

            if (absX <= 0f && absY <= 0f)
            {
                return Direction.None;
            }

            var dominant = absX >= absY
                ? (gridDirection.x > 0f ? Direction.East : Direction.West)
                : (gridDirection.y > 0f ? Direction.North : Direction.South);

            // Nothing to hold on to, or the stick agrees with where we are already going.
            if (current == Direction.None || dominant == current)
            {
                return dominant;
            }

            // Reversing stays on the same axis and the same lane, so it needs no margin.
            if (dominant == current.Opposite())
            {
                return dominant;
            }

            var currentAxis = current.IsSameAxis(Direction.East) ? absX : absY;
            var candidateAxis = dominant.IsSameAxis(Direction.East) ? absX : absY;

            return candidateAxis > currentAxis * switchRatio ? dominant : current;
        }
    }
}
