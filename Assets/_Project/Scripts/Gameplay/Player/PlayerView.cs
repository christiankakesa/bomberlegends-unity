using BomberLegends.Core;
using BomberLegends.Gameplay.Board;
using UnityEngine;

namespace BomberLegends.Gameplay.Player
{
    /// <summary>
    /// Draws the player at an interpolated position between two simulation ticks.
    /// </summary>
    /// <remarks>
    /// The simulation runs at a fixed rate well below the display rate, so drawing the raw tick
    /// position would visibly step. Interpolating between the previous and current tick is what
    /// makes movement look smooth without the rules having to run any faster.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Renderer drawn at the interpolated position.")]
        private SpriteRenderer? _renderer;

        [SerializeField]
        [Tooltip("Placeholder tint until the character art exists.")]
        private Color _colour = new Color(0.95f, 0.85f, 0.25f);

        [SerializeField, Range(0.1f, 2f)]
        [Tooltip("Size relative to one tile width.")]
        private float _scale = 0.55f;

        private IsometricProjector _projector = null!;

        /// <summary>Prepares the view for a match.</summary>
        public void Initialise(IsometricProjector projector)
        {
            _projector = projector;

            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _renderer.sprite = PlaceholderArt.Disc;
            _renderer.color = _colour;
            _renderer.transform.localScale = Vector3.one * _scale;
        }

        /// <summary>
        /// Places the view between two tick positions.
        /// </summary>
        /// <param name="previous">Position at the previous tick.</param>
        /// <param name="current">Position at the current tick.</param>
        /// <param name="alpha">How far through the current tick the frame is, from zero to one.</param>
        public void Render(SubTilePoint previous, SubTilePoint current, float alpha)
        {
            if (_renderer == null)
            {
                return;
            }

            var gridX = Mathf.LerpUnclamped(ToGrid(previous.X), ToGrid(current.X), alpha);
            var gridY = Mathf.LerpUnclamped(ToGrid(previous.Y), ToGrid(current.Y), alpha);

            var world = _projector.GridToWorld(gridX, gridY);
            transform.localPosition = new Vector3(world.x, world.y, 0f);

            // Sorted from the interpolated grid depth, so the player passes behind and in front of
            // blocks at exactly the right moment rather than popping at tile boundaries.
            _renderer.sortingOrder = IsometricProjector.SortingOrder(gridX, gridY);
        }

        private static float ToGrid(int subTileUnits) =>
            ((float)subTileUnits / SubTilePoint.UnitsPerTile) - 0.5f;
    }
}
