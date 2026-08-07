using UnityEngine;

namespace BomberLegends.Gameplay.Enemies
{
    /// <summary>
    /// Draws one enemy, flashing white while it is briefly immune after a hit.
    /// </summary>
    /// <remarks>
    /// The flash is not decoration: the immunity window is a rule the player has to be able to read,
    /// or repeated hits that land on nothing look like the game ignoring them.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class EnemyView : MonoBehaviour
    {
        private static readonly int BaseColour = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color _colour = new Color(0.85f, 0.22f, 0.32f);
        [SerializeField, Range(0.2f, 1.5f)] private float _diameter = 0.66f;
        [SerializeField, Range(0.2f, 2f)] private float _height = 0.8f;

        private MeshRenderer _renderer = null!;
        private MaterialPropertyBlock _properties = null!;

        /// <summary>Places the enemy and resets its appearance.</summary>
        public void Begin(Vector3 position)
        {
            Ensure();

            transform.localPosition = position;
            transform.localScale = new Vector3(_diameter, _height, _diameter);
            SetColour(_colour);
        }

        /// <summary>Moves the enemy and reflects whether it is currently immune.</summary>
        public void Render(Vector3 position, bool invulnerable)
        {
            Ensure();

            transform.localPosition = position;
            SetColour(invulnerable ? Color.Lerp(_colour, Color.white, 0.7f) : _colour);
        }

        /// <summary>Restores a clean state before reuse.</summary>
        public void ResetView()
        {
            Ensure();
            SetColour(_colour);
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
