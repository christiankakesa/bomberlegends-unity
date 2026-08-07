using BomberLegends.Core;
using BomberLegends.Simulation.Board;
using UnityEngine;

namespace BomberLegends.Gameplay.Board
{
    /// <summary>
    /// Builds the arena geometry from the simulation's tile grid.
    /// </summary>
    /// <remarks>
    /// Blocks are cubes standing on a flat floor, so depth reads from the geometry itself and no
    /// sorting arithmetic is needed. Tiles are static, so every renderer is created once and nothing
    /// touches them per frame; destruction is handled by hiding the block that a simulation event
    /// names.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BoardRenderer : MonoBehaviour
    {
        [Header("Colours")]
        [SerializeField] private Color _floorColour = new Color(0.20f, 0.19f, 0.30f);
        [SerializeField] private Color _floorAlternateColour = new Color(0.24f, 0.23f, 0.36f);
        [SerializeField] private Color _solidColour = new Color(0.13f, 0.52f, 0.60f);
        [SerializeField] private Color _destructibleColour = new Color(0.85f, 0.45f, 0.16f);

        private GameObject?[] _blocks = System.Array.Empty<GameObject>();
        private Material?[] _materials = System.Array.Empty<Material>();
        private BoardProjector _projector = null!;
        private int _width;

        /// <summary>Builds the arena. Safe to call again for a new level.</summary>
        public void Build(in BoardState board, BoardProjector projector)
        {
            Clear();

            _projector = projector;
            _width = board.Width;
            _blocks = new GameObject?[board.Width * board.Height];

            var floorA = PlaceholderMeshes.CreateMaterial(_floorColour);
            var floorB = PlaceholderMeshes.CreateMaterial(_floorAlternateColour);
            var solid = PlaceholderMeshes.CreateMaterial(_solidColour);
            var destructible = PlaceholderMeshes.CreateMaterial(_destructibleColour);
            _materials = new[] { floorA, floorB, solid, destructible };

            for (var y = 0; y < board.Height; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    var tile = new GridCoord(x, y);
                    CreateFloor(tile, (x + y) % 2 == 0 ? floorA : floorB);

                    var type = board[tile];
                    if (type != TileType.Empty)
                    {
                        CreateBlock(tile, type == TileType.Solid ? solid : destructible);
                    }
                }
            }
        }

        /// <summary>Updates one tile after the simulation changed it.</summary>
        public void SetTile(GridCoord tile, TileType type)
        {
            var index = tile.ToIndex(_width);
            if (index < 0 || index >= _blocks.Length)
            {
                return;
            }

            var block = _blocks[index];
            if (block != null)
            {
                block.SetActive(type != TileType.Empty);
            }
        }

        private void OnDestroy() => Clear();

        private void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            for (var i = 0; i < _materials.Length; i++)
            {
                if (_materials[i] != null)
                {
                    Destroy(_materials[i]);
                }
            }

            _blocks = System.Array.Empty<GameObject>();
            _materials = System.Array.Empty<Material>();
        }

        private void CreateFloor(GridCoord tile, Material material)
        {
            var floor = CreateRenderer($"Floor {tile.X},{tile.Y}", PlaceholderMeshes.Quad, material);

            floor.transform.localPosition = _projector.TileToWorld(tile);

            // A quad faces the camera by default; rotate it flat so it becomes ground.
            floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = Vector3.one * _projector.TileSize;
        }

        private void CreateBlock(GridCoord tile, Material material)
        {
            var height = _projector.BlockHeight;
            var block = CreateRenderer($"Block {tile.X},{tile.Y}", PlaceholderMeshes.Cube, material);

            block.transform.localPosition = _projector.TileToWorld(tile, height * 0.5f);
            block.transform.localScale = new Vector3(_projector.TileSize, height, _projector.TileSize);

            _blocks[tile.ToIndex(_width)] = block;
        }

        private GameObject CreateRenderer(string name, Mesh mesh, Material material)
        {
            var child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);

            child.GetComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            return child;
        }
    }
}
