using BomberLegends.Core;
using BomberLegends.Gameplay.Board;
using UnityEngine;

namespace BomberLegends.Gameplay.Player
{
    /// <summary>
    /// Draws the player at an interpolated position between two simulation ticks.
    /// </summary>
    /// <remarks>
    /// The simulation runs well below the display rate, so drawing the raw tick position would step
    /// visibly. Interpolating is what makes movement look smooth without the rules running faster.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private Color _colour = new Color(0.95f, 0.82f, 0.25f);
        [SerializeField, Range(0.2f, 1.5f)] private float _diameter = 0.7f;
        [SerializeField, Range(0.2f, 2f)] private float _height = 0.9f;

        private BoardProjector _projector = null!;
        private Transform? _body;
        private Material? _material;

        /// <summary>The player's current world position, for the camera to follow.</summary>
        public Vector3 WorldPosition { get; private set; }

        /// <summary>Prepares the view for a match.</summary>
        public void Initialise(BoardProjector projector)
        {
            _projector = projector;

            if (_body != null)
            {
                return;
            }

            _material = PlaceholderMeshes.CreateMaterial(_colour);

            var body = new GameObject("Body", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(transform, false);
            body.GetComponent<MeshFilter>().sharedMesh = PlaceholderMeshes.Sphere;
            body.GetComponent<MeshRenderer>().sharedMaterial = _material;
            body.transform.localScale = new Vector3(_diameter, _height, _diameter);

            // Raised so the body rests on the floor rather than sinking half-way through it.
            body.transform.localPosition = new Vector3(0f, _height * 0.5f, 0f);

            _body = body.transform;
        }

        /// <summary>Places the view between two tick positions.</summary>
        public void Render(SubTilePoint previous, SubTilePoint current, float alpha)
        {
            var gridX = Mathf.LerpUnclamped(
                BoardProjector.ToGrid(previous.X), BoardProjector.ToGrid(current.X), alpha);
            var gridY = Mathf.LerpUnclamped(
                BoardProjector.ToGrid(previous.Y), BoardProjector.ToGrid(current.Y), alpha);

            WorldPosition = _projector.GridToWorld(gridX, gridY);
            transform.localPosition = WorldPosition;
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
