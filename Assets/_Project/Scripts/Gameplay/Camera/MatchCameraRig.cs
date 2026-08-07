using BomberLegends.Gameplay.Board;
using UnityEngine;

namespace BomberLegends.Gameplay.Camera
{
    /// <summary>
    /// A tilted camera that follows the player and stays inside the arena.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Replaces the fixed frame used while arenas were single-screen. Arenas may now be larger than
    /// the view, so the camera tracks the player — but it is clamped to the arena so the player never
    /// sees past the edge of the world, which is the usual failure of a naive follower.
    /// </para>
    /// <para>
    /// Following is smoothed and frame-rate independent. It runs in <c>LateUpdate</c> so it reads the
    /// player's position after the match has finished moving them; doing it in <c>Update</c> leaves
    /// the camera a frame behind and reads as a subtle judder that is very hard to diagnose later.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MatchCameraRig : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Camera to drive. Falls back to the camera on this object.")]
        private UnityEngine.Camera? _camera;

        [Header("Framing")]
        [SerializeField, Range(20f, 89f)]
        [Tooltip("Downward tilt. Higher looks more top-down; lower shows more of the horizon.")]
        private float _pitch = 55f;

        [SerializeField, Range(4f, 40f)]
        [Tooltip("Distance from the point being followed.")]
        private float _distance = 17f;

        [Header("Following")]
        [SerializeField, Range(0.01f, 1f)]
        [Tooltip("Seconds to close most of the gap to the target. Lower is snappier.")]
        private float _smoothing = 0.16f;

        [SerializeField, Range(0f, 8f)]
        [Tooltip("Empty space kept beyond the arena edge, in world units.")]
        private float _margin = 2f;

        [SerializeField]
        [Tooltip("Pull back further on larger arenas so the same share of the board stays visible. " +
                 "Off keeps the distance fixed however big the arena is.")]
        private bool _scaleDistanceWithArena = true;

        [SerializeField, Range(10f, 60f)]
        [Tooltip("Arena width, in tiles, that the configured distance is tuned for.")]
        private float _referenceArenaWidth = 25f;

        [SerializeField, Range(1f, 2f)]
        [Tooltip("Ceiling on how far the automatic pull-back may go, as a multiple of the distance.")]
        private float _maxDistanceScale = 1.35f;

        private Vector3 _focus;
        private float _appliedDistance;
        private Vector3 _velocity;
        private Bounds _limits;
        private bool _hasLimits;

        /// <summary>Sets up the rig for an arena and snaps straight to the starting point.</summary>
        public void Begin(int boardWidth, int boardHeight, BoardProjector projector, Vector3 target)
        {
            _limits = projector.BoardBounds(boardWidth, boardHeight);
            _hasLimits = true;
            _appliedDistance = ResolveDistance(boardWidth);
            _focus = Clamp(target);
            _velocity = Vector3.zero;

            Apply();
        }

        /// <summary>Moves the camera towards the target. Called once the player has been placed.</summary>
        public void Follow(Vector3 target, float deltaSeconds)
        {
            if (!_hasLimits)
            {
                return;
            }

            _focus = Vector3.SmoothDamp(_focus, Clamp(target), ref _velocity, _smoothing, Mathf.Infinity,
                deltaSeconds);

            Apply();
        }

        private Vector3 Clamp(Vector3 target)
        {
            // The visible half-extents at this distance and tilt, so the clamp keeps the arena edge
            // at the screen edge rather than letting the void come into view.
            var camera = Resolve();
            if (camera == null)
            {
                return target;
            }

            var halfHeight = _appliedDistance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var halfWidth = halfHeight * camera.aspect;

            var limitX = Mathf.Max(0f, (_limits.extents.x + _margin) - halfWidth);
            var limitZ = Mathf.Max(0f, (_limits.extents.z + _margin) - (halfHeight * 0.75f));

            return new Vector3(
                Mathf.Clamp(target.x, _limits.center.x - limitX, _limits.center.x + limitX),
                0f,
                Mathf.Clamp(target.z, _limits.center.z - limitZ, _limits.center.z + limitZ));
        }

        private void Apply()
        {
            var camera = Resolve();
            if (camera == null)
            {
                return;
            }

            var rotation = Quaternion.Euler(_pitch, 0f, 0f);

            camera.transform.SetPositionAndRotation(
                _focus - (rotation * Vector3.forward * _appliedDistance),
                rotation);
        }

        /// <summary>
        /// How far back to sit for an arena of this size.
        /// </summary>
        /// <remarks>
        /// A fixed distance that frames a small arena well leaves a large one feeling claustrophobic,
        /// but scaling without a ceiling turns the player into a speck. The cap keeps the character
        /// readable however large the arena grows.
        /// </remarks>
        private float ResolveDistance(int boardWidth)
        {
            if (!_scaleDistanceWithArena || _referenceArenaWidth <= 0f)
            {
                return _distance;
            }

            var scale = Mathf.Clamp(boardWidth / _referenceArenaWidth, 1f, _maxDistanceScale);
            return _distance * scale;
        }

        private UnityEngine.Camera? Resolve() =>
            _camera != null ? _camera : _camera = GetComponent<UnityEngine.Camera>();
    }
}
