using UnityEngine;

namespace BomberLegends.Gameplay.Skills
{
    /// <summary>
    /// Draws one skillshot in flight, with a trail so it can be seen at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A playtester reported the skillshot as broken. It was not — the bullet was simply
    /// unreadable, and for three reasons at once: it was under a third the size of a block, it was
    /// light cyan against teal blocks, and at a 55° camera pitch a one-unit block hides roughly a
    /// third of a tile behind it, which is where a projectile at half block height spends its life.
    /// </para>
    /// <para>
    /// The trail is the fix that matters. A shape crossing twelve tiles a second is legible from the
    /// line it leaves, not from where it happens to be on any one frame — and the line survives the
    /// moments the shot passes behind something.
    /// </para>
    /// <para>
    /// Ghosts shrink rather than fade because the greybox material is opaque; alpha would need a
    /// transparent variant and therefore another shader to keep alive through build stripping. It
    /// also suits the lore, which describes Soul Orbs as leaving afterimages.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ProjectileView : MonoBehaviour
    {
        private static readonly int BaseColour = Shader.PropertyToID("_BaseColor");

        /// <summary>How many afterimages follow the head.</summary>
        private const int TrailLength = 6;

        [SerializeField]
        [Tooltip("Deliberately off the arena's palette. Teal reads as part of the wall.")]
        private Color _colour = new Color(1f, 0.45f, 0.92f);

        [SerializeField, Range(0.1f, 0.8f)]
        [Tooltip("Diameter in world units. A tile is one unit and a block fills 0.88 of it.")]
        private float _diameter = 0.42f;

        private MeshRenderer _renderer = null!;
        private MaterialPropertyBlock _properties = null!;
        private Transform[] _trail = System.Array.Empty<Transform>();
        private int _written;

        /// <summary>Places the projectile and gives it its appearance.</summary>
        public void Begin(Vector3 position)
        {
            Ensure();

            transform.localPosition = position;
            transform.localScale = Vector3.one * _diameter;

            _properties.SetColor(BaseColour, _colour);
            _renderer.SetPropertyBlock(_properties);

            BuildTrail();
            ResetTrail(position);
            ShowTrail(true);
        }

        /// <summary>Moves the projectile and drags its trail along behind it.</summary>
        public void Render(Vector3 position)
        {
            transform.localPosition = position;

            if (_trail.Length == 0)
            {
                return;
            }

            // Each ghost takes the one in front's place, so the trail is the path actually flown
            // rather than a guess extrapolated from a direction.
            for (var i = _trail.Length - 1; i > 0; i--)
            {
                _trail[i].position = _trail[i - 1].position;
            }

            _trail[0].position = position;

            if (_written < _trail.Length)
            {
                _written++;
            }
        }

        /// <summary>Restores a clean state before reuse.</summary>
        /// <remarks>
        /// The trail must be collapsed as well as the head reset, or a pooled projectile streaks
        /// across the arena from wherever its previous life ended.
        /// </remarks>
        public void ResetView()
        {
            Ensure();

            transform.localScale = Vector3.one * _diameter;
            ResetTrail(transform.localPosition);
        }

        /// <summary>
        /// Hides the trail with the shot it belongs to.
        /// </summary>
        /// <remarks>
        /// The ghosts are siblings rather than children — they have to stay where they were dropped
        /// instead of being dragged along — which means returning the head to its pool does not hide
        /// them. Without this, every spent shot leaves a permanent line of dots across the arena.
        /// </remarks>
        private void OnDisable() => ShowTrail(false);

        private void ShowTrail(bool visible)
        {
            for (var i = 0; i < _trail.Length; i++)
            {
                if (_trail[i] != null && _trail[i].gameObject.activeSelf != visible)
                {
                    _trail[i].gameObject.SetActive(visible);
                }
            }
        }

        private void ResetTrail(Vector3 position)
        {
            _written = 0;

            for (var i = 0; i < _trail.Length; i++)
            {
                _trail[i].position = position;
            }
        }

        private void BuildTrail()
        {
            if (_trail.Length > 0)
            {
                return;
            }

            var mesh = GetComponent<MeshFilter>().sharedMesh;
            var material = _renderer.sharedMaterial;

            _trail = new Transform[TrailLength];

            for (var i = 0; i < TrailLength; i++)
            {
                var ghost = new GameObject($"Trail {i}", typeof(MeshFilter), typeof(MeshRenderer));

                // Parented to the arena rather than to the head, so a ghost stays where it was
                // dropped instead of being dragged along by its parent.
                ghost.transform.SetParent(transform.parent, false);
                ghost.GetComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = ghost.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // Tapering to a point, which reads as direction as well as speed.
                var taper = 1f - ((i + 1f) / (TrailLength + 1f));
                ghost.transform.localScale = Vector3.one * _diameter * taper;

                var properties = new MaterialPropertyBlock();
                properties.SetColor(BaseColour, Color.Lerp(_colour, Color.white, 0.35f * taper));
                renderer.SetPropertyBlock(properties);

                _trail[i] = ghost.transform;
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < _trail.Length; i++)
            {
                if (_trail[i] != null)
                {
                    Destroy(_trail[i].gameObject);
                }
            }
        }

        private void Ensure()
        {
            _renderer ??= GetComponent<MeshRenderer>();
            _properties ??= new MaterialPropertyBlock();
        }
    }
}
