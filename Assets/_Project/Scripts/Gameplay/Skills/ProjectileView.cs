using UnityEngine;

namespace BomberLegends.Gameplay.Skills
{
    /// <summary>
    /// Draws one skillshot in flight.
    /// </summary>
    /// <remarks>
    /// Deliberately small and bright. A skillshot has to be legible to the person who fired it —
    /// they need to see whether it will connect while it is still travelling — without ever being
    /// mistaken for a bomb, which is the one round object in this game that must never be
    /// misread.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ProjectileView : MonoBehaviour
    {
        private static readonly int BaseColour = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color _colour = new Color(0.45f, 0.85f, 1f);
        [SerializeField, Range(0.1f, 0.8f)] private float _diameter = 0.28f;

        private MeshRenderer _renderer = null!;
        private MaterialPropertyBlock _properties = null!;

        /// <summary>Places the projectile and gives it its appearance.</summary>
        public void Begin(Vector3 position)
        {
            Ensure();

            transform.localPosition = position;
            transform.localScale = Vector3.one * _diameter;

            _properties.SetColor(BaseColour, _colour);
            _renderer.SetPropertyBlock(_properties);
        }

        /// <summary>Moves the projectile.</summary>
        public void Render(Vector3 position) => transform.localPosition = position;

        /// <summary>Restores a clean state before reuse.</summary>
        public void ResetView()
        {
            Ensure();
            transform.localScale = Vector3.one * _diameter;
        }

        private void Ensure()
        {
            _renderer ??= GetComponent<MeshRenderer>();
            _properties ??= new MaterialPropertyBlock();
        }
    }
}
