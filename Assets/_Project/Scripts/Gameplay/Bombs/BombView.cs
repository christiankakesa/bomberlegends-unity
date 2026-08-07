using UnityEngine;

namespace BomberLegends.Gameplay.Bombs
{
    /// <summary>
    /// A bomb sitting on the board, pulsing faster as its fuse runs down.
    /// </summary>
    /// <remarks>
    /// The pulse is the only warning a player gets about timing, so it is driven by the fuse itself
    /// rather than a fixed animation: a bomb with one second left must look different from one with
    /// three, whatever the fuse is tuned to.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class BombView : MonoBehaviour
    {
        private static readonly int BaseColour = Shader.PropertyToID("_BaseColor");

        [SerializeField, Range(0.2f, 1.2f)] private float _diameter = 0.62f;
        [SerializeField] private Color _calmColour = new Color(0.16f, 0.17f, 0.24f);
        [SerializeField] private Color _urgentColour = new Color(0.95f, 0.32f, 0.22f);
        [SerializeField, Range(1f, 16f)] private float _maxPulseRate = 9f;

        private MeshRenderer _renderer = null!;
        private MaterialPropertyBlock _properties = null!;
        private float _pulsePhase;

        /// <summary>Places the bomb and resets its pulse.</summary>
        public void Begin(Vector3 position)
        {
            Ensure();

            transform.localPosition = position;
            transform.localScale = Vector3.one * _diameter;
            _pulsePhase = 0f;

            SetColour(_calmColour);
        }

        /// <summary>Advances the pulse. <paramref name="fuseProgress"/> runs from zero to one.</summary>
        public void Advance(float deltaSeconds, float fuseProgress)
        {
            Ensure();

            var urgency = Mathf.Clamp01(fuseProgress);
            _pulsePhase += deltaSeconds * Mathf.Lerp(1.5f, _maxPulseRate, urgency);

            var pulse = (Mathf.Sin(_pulsePhase * Mathf.PI * 2f) + 1f) * 0.5f;

            SetColour(Color.Lerp(_calmColour, _urgentColour, urgency * pulse));
            transform.localScale = Vector3.one * (_diameter * Mathf.Lerp(1f, 1.14f, urgency * pulse));
        }

        /// <summary>Restores a clean state before reuse.</summary>
        public void ResetView()
        {
            Ensure();

            transform.localScale = Vector3.one * _diameter;
            _pulsePhase = 0f;
            SetColour(_calmColour);
        }

        private void SetColour(Color colour)
        {
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
