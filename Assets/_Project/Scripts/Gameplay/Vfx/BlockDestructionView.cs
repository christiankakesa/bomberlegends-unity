using UnityEngine;

namespace BomberLegends.Gameplay.Vfx
{
    /// <summary>A block coming apart: a brief expanding flash where it stood.</summary>
    public sealed class BlockDestructionView : TimedMeshView
    {
        private Vector3 _baseScale = Vector3.one;

        /// <summary>Records the scale to expand from.</summary>
        public void BeginAt(Vector3 position, float duration, Color colour, Vector3 scale)
        {
            _baseScale = scale;
            Begin(position, duration, colour, scale);
        }

        /// <inheritdoc />
        protected override void Apply(float progress)
        {
            SetColour(1f - progress);
            transform.localScale = _baseScale * Mathf.Lerp(1f, 1.5f, progress);
        }
    }
}
