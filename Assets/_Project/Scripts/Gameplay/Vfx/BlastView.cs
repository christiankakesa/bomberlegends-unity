using UnityEngine;

namespace BomberLegends.Gameplay.Vfx
{
    /// <summary>One tile of fire, fading as the blast burns out.</summary>
    /// <remarks>
    /// Its duration comes from the simulation's lethal window, so what looks dangerous and what
    /// actually kills you are the same thing. Letting those drift apart is the fastest way to make a
    /// player feel cheated.
    /// </remarks>
    public sealed class BlastView : TimedMeshView
    {
        /// <inheritdoc />
        protected override void Apply(float progress)
        {
            // Full strength for most of its life, then a quick fade, so the lethal window reads as
            // lethal for exactly as long as it is.
            SetColour(progress < 0.6f ? 1f : 1f - ((progress - 0.6f) / 0.4f));
        }
    }
}
