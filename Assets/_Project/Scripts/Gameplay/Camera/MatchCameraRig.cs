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
        /// <summary>World units a full-strength shake displaces the camera by.</summary>
        /// <remarks>
        /// Small on purpose. A blast tile is lethal and the player has to be able to read the board
        /// through the shake; anything that obscures which tile is on fire costs a life.
        /// </remarks>
        private const float MaxShakeUnits = 0.34f;

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
        private float _shakeStrength;
        private float _shakeRemaining;
        private float _shakeDuration;
        private uint _shakeSeed = 0x6D2B79F5u;
        private Bounds _limits;
        private bool _hasLimits;

        /// <summary>The camera being driven, for anything that needs to unproject the pointer.</summary>
        public UnityEngine.Camera? Camera => Resolve();

        /// <summary>
        /// Knocks the camera, settling back over the given time.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Applied to the camera alone and never to the rig's focus, so a shake cannot drag the
        /// framing off the player or feed back into the follow. The simulation is not touched at
        /// all — a shake that moved anything the rules read would be a physics bug that only
        /// appeared on impact.
        /// </para>
        /// <para>
        /// A stronger knock overrides a weaker one still settling. Adding them together lets a
        /// chain reaction stack into something unreadable at exactly the moment the player most
        /// needs to see the board.
        /// </para>
        /// </remarks>
        public void Shake(float strength, float seconds)
        {
            if (strength <= 0f || seconds <= 0f || strength < _shakeStrength * (_shakeRemaining / Mathf.Max(0.0001f, _shakeDuration)))
            {
                return;
            }

            _shakeStrength = Mathf.Clamp01(strength);
            _shakeDuration = seconds;
            _shakeRemaining = seconds;
        }

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

            if (_shakeRemaining > 0f)
            {
                _shakeRemaining -= deltaSeconds;
            }

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
                _focus - (rotation * Vector3.forward * _appliedDistance) + ShakeOffset(),
                rotation);
        }

        /// <summary>
        /// The camera's displacement from the shake still settling, if any.
        /// </summary>
        /// <remarks>
        /// Faded out with the square of what remains rather than linearly, so a knock lands hard and
        /// then gets out of the way. A linear fade reads as the camera being loose.
        /// </remarks>
        private Vector3 ShakeOffset()
        {
            if (_shakeRemaining <= 0f || _shakeDuration <= 0f)
            {
                return Vector3.zero;
            }

            var progress = Mathf.Clamp01(_shakeRemaining / _shakeDuration);
            var amount = _shakeStrength * progress * progress * MaxShakeUnits;

            return new Vector3(NextOffset(), NextOffset(), 0f) * amount;
        }

        /// <summary>A value between minus one and one, from the rig's own generator.</summary>
        /// <remarks>
        /// Never the simulation's generator. Drawing from that would let the camera change the
        /// outcome of a match, and would make a replay depend on how many frames were drawn.
        /// </remarks>
        private float NextOffset()
        {
            _shakeSeed ^= _shakeSeed << 13;
            _shakeSeed ^= _shakeSeed >> 17;
            _shakeSeed ^= _shakeSeed << 5;

            return ((_shakeSeed & 0xFFFF) / 32768f) - 1f;
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
