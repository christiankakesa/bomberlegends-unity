using BomberLegends.Simulation;
using UnityEngine;

namespace BomberLegends.Input
{
    /// <summary>Packs a grid-space aim direction into the two bytes an intent carries.</summary>
    public static class PointerAim
    {
        /// <summary>
        /// Quantises an aim direction to the intent's axis range.
        /// </summary>
        /// <remarks>
        /// Normalised first, so a pointer far from the player aims no harder than one just outside
        /// them — magnitude carries no meaning, only direction does. Rounding to whole numbers is
        /// what keeps the intent reproducible across devices.
        /// </remarks>
        public static bool TryPack(float gridX, float gridY, out sbyte aimX, out sbyte aimY)
        {
            aimX = 0;
            aimY = 0;

            var lengthSquared = (gridX * gridX) + (gridY * gridY);

            if (lengthSquared <= Mathf.Epsilon)
            {
                return false;
            }

            var scale = PlayerIntent.AxisRange / Mathf.Sqrt(lengthSquared);

            aimX = (sbyte)Mathf.Clamp(
                Mathf.RoundToInt(gridX * scale), -PlayerIntent.AxisRange, PlayerIntent.AxisRange);
            aimY = (sbyte)Mathf.Clamp(
                Mathf.RoundToInt(gridY * scale), -PlayerIntent.AxisRange, PlayerIntent.AxisRange);

            return aimX != 0 || aimY != 0;
        }
    }
}
