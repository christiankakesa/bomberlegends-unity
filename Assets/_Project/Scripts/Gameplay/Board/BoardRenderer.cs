using BomberLegends.Core;
using BomberLegends.Simulation.Board;
using UnityEngine;

namespace BomberLegends.Gameplay.Board
{
    /// <summary>
    /// Draws the board once, from the simulation's tile grid.
    /// </summary>
    /// <remarks>
    /// Tiles are static for the whole of Milestone 1, so every renderer is created and sorted at
    /// build time and nothing touches them per frame. When blocks start being destroyed in
    /// Milestone 2, individual tiles are updated in response to simulation events rather than by
    /// rebuilding the board.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BoardRenderer : MonoBehaviour
    {
        [Header("Colours")]
        [SerializeField]
        [Tooltip("Walkable floor.")]
        private Color _floorColour = new Color(0.16f, 0.14f, 0.28f);

        [SerializeField]
        [Tooltip("Alternating floor shade, so the grid is legible while moving.")]
        private Color _floorAlternateColour = new Color(0.20f, 0.17f, 0.34f);

        [SerializeField]
        [Tooltip("Permanent structure.")]
        private Color _solidColour = new Color(0.10f, 0.55f, 0.62f);

        [SerializeField]
        [Tooltip("Destructible block.")]
        private Color _destructibleColour = new Color(0.85f, 0.45f, 0.16f);

        [Header("Blocks")]
        [SerializeField, Range(0f, 1f)]
        [Tooltip("How far a block is lifted off the floor, in tile heights, to read as standing up.")]
        private float _blockLift = 0.35f;

        private SpriteRenderer[] _blockRenderers = System.Array.Empty<SpriteRenderer>();
        private IsometricProjector _projector = null!;
        private int _width;

        /// <summary>Builds the board's renderers. Safe to call again to rebuild for a new level.</summary>
        public void Build(in BoardState board, IsometricProjector projector)
        {
            Clear();

            _projector = projector;
            _width = board.Width;
            _blockRenderers = new SpriteRenderer[board.Width * board.Height];

            for (var y = 0; y < board.Height; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    var tile = new GridCoord(x, y);
                    CreateFloor(tile);
                    CreateBlock(tile, board[tile]);
                }
            }
        }

        /// <summary>Updates one tile after the simulation changed it.</summary>
        public void SetTile(GridCoord tile, TileType type)
        {
            var index = tile.ToIndex(_width);
            if (index < 0 || index >= _blockRenderers.Length)
            {
                return;
            }

            var renderer = _blockRenderers[index];
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = type != TileType.Empty;
            renderer.color = type == TileType.Solid ? _solidColour : _destructibleColour;
        }

        private void OnDestroy() => Clear();

        private void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            _blockRenderers = System.Array.Empty<SpriteRenderer>();
        }

        private void CreateFloor(GridCoord tile)
        {
            var renderer = CreateRenderer($"Floor {tile.X},{tile.Y}", tile, lift: 0f);
            renderer.sprite = PlaceholderArt.Diamond;
            renderer.color = (tile.X + tile.Y) % 2 == 0 ? _floorColour : _floorAlternateColour;
            renderer.sortingOrder = IsometricProjector.FloorSortingOrder(tile);
        }

        private void CreateBlock(GridCoord tile, TileType type)
        {
            var renderer = CreateRenderer($"Block {tile.X},{tile.Y}", tile, _blockLift);
            renderer.sprite = PlaceholderArt.Diamond;
            renderer.color = type == TileType.Solid ? _solidColour : _destructibleColour;
            renderer.sortingOrder = IsometricProjector.SortingOrder(tile.X, tile.Y);
            renderer.enabled = type != TileType.Empty;

            _blockRenderers[tile.ToIndex(_width)] = renderer;
        }

        private SpriteRenderer CreateRenderer(string name, GridCoord tile, float lift)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);

            var world = _projector.TileToWorld(tile);
            child.transform.localPosition = new Vector3(world.x, world.y + (lift * _projector.TileHeight), 0f);

            return child.AddComponent<SpriteRenderer>();
        }
    }
}
