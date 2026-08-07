using BomberLegends.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BomberLegends.Gameplay.Player
{
    /// <summary>
    /// Aims wherever the mouse pointer is resting on the arena floor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lives in Gameplay because it needs the camera and the player's world position, neither of
    /// which the Input layer may reference. It satisfies an interface Input declared, so the
    /// dependency still runs one way.
    /// </para>
    /// <para>
    /// The ground plane sits at the player's own height rather than at zero, so the point under the
    /// cursor is the point the shot will pass through. Intersecting the floor instead would make
    /// every shot land slightly short of where the player pointed — a bias small enough to survive
    /// review and large enough to make aiming feel wrong.
    /// </para>
    /// </remarks>
    public sealed class PointerAimSource : IAimSource
    {
        private readonly UnityEngine.Camera _camera;
        private readonly PlayerView _player;

        /// <summary>Creates an aim source reading the given camera and player.</summary>
        public PointerAimSource(UnityEngine.Camera camera, PlayerView player)
        {
            _camera = camera;
            _player = player;
        }

        /// <inheritdoc />
        public bool TryGetAim(out float gridX, out float gridY)
        {
            gridX = 0f;
            gridY = 0f;

            var mouse = Mouse.current;

            if (mouse == null || _camera == null || _player == null)
            {
                return false;
            }

            var origin = _player.WorldPosition;
            var plane = new Plane(Vector3.up, origin);
            var ray = _camera.ScreenPointToRay(mouse.position.ReadValue());

            if (!plane.Raycast(ray, out var distance))
            {
                return false;
            }

            var point = ray.GetPoint(distance);

            // The board is projected onto the XZ plane with grid Y running along world Z, so a
            // world-space delta is already a grid-space direction once the scale is dropped — and
            // only the direction survives quantisation, so the scale never matters.
            gridX = point.x - origin.x;
            gridY = point.z - origin.z;

            return (gridX * gridX) + (gridY * gridY) > 0.0001f;
        }
    }
}
