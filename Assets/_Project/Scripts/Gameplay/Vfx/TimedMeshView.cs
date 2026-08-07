using UnityEngine;

namespace BomberLegends.Gameplay.Vfx
{
    /// <summary>
    /// A short-lived mesh effect that reports when it is finished.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Advanced by its owner rather than by a coroutine or an <c>Update</c> of its own. Dozens are
    /// alive during a chain reaction, and a per-object update plus a coroutine each would cost more
    /// than the effect is worth. Reporting completion instead of self-destroying is what lets the
    /// owner pool it.
    /// </para>
    /// <para>
    /// Colour is set through a <see cref="MaterialPropertyBlock"/> so every instance shares one
    /// material. Giving each its own would break batching and leak a material per pooled object.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public abstract class TimedMeshView : MonoBehaviour
    {
        private static readonly int BaseColour = Shader.PropertyToID("_BaseColor");

        private MeshRenderer _renderer = null!;
        private MaterialPropertyBlock _properties = null!;
        private Color _colour;
        private float _elapsed;
        private float _duration;

        /// <summary>Starts the effect at a position, for a duration in seconds.</summary>
        public void Begin(Vector3 position, float duration, Color colour, Vector3 scale)
        {
            Ensure();

            transform.localPosition = position;
            transform.localScale = scale;
            _colour = colour;
            _elapsed = 0f;
            _duration = Mathf.Max(0.01f, duration);

            Apply(0f);
        }

        /// <summary>Advances the effect.</summary>
        /// <returns><see langword="false"/> once it has finished and may be reused.</returns>
        public bool Advance(float deltaSeconds)
        {
            _elapsed += deltaSeconds;

            var progress = Mathf.Clamp01(_elapsed / _duration);
            Apply(progress);

            return progress < 1f;
        }

        /// <summary>Restores a clean state before reuse.</summary>
        /// <remarks>
        /// Pooled objects that keep scale, rotation or colour from their previous use are the classic
        /// pooling bug, so this runs on every release.
        /// </remarks>
        public void ResetView()
        {
            Ensure();

            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            _elapsed = 0f;
        }

        /// <summary>Applies the effect's appearance at a normalised point through its life.</summary>
        protected abstract void Apply(float progress);

        /// <summary>Sets the rendered colour, including alpha.</summary>
        protected void SetColour(float alpha)
        {
            var colour = _colour;
            colour.a = alpha;

            _properties.SetColor(BaseColour, colour);
            _renderer.SetPropertyBlock(_properties);
        }

        private void Ensure()
        {
            _renderer ??= GetComponent<MeshRenderer>();
            _properties ??= new MaterialPropertyBlock();
        }
    }
}
