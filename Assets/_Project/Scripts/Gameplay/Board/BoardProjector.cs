using BomberLegends.Core;
using BomberLegends.Input;
using UnityEngine;

namespace BomberLegends.Gameplay.Board
{
    /// <summary>
    /// Places the board in the world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The grid lies flat on the XZ plane with Y as height, which is what a tilted 3D camera expects.
    /// Depth ordering is now the depth buffer's job, so the sorting-order arithmetic the 2D version
    /// needed is gone entirely.
    /// </para>
    /// <para>
    /// Named for its job rather than its projection, because the projection has now changed twice and
    /// the simulation has been indifferent to it every time.
    /// </para>
    /// </remarks>
    public sealed class BoardProjector : IGridProjection
    {
        /// <summary>Creates a projector.</summary>
        /// <param name="tileSize">World units across one tile.</param>
        /// <param name="blockHeight">How tall a standing block is.</param>
        public BoardProjector(float tileSize = 1f, float blockHeight = 1f)
        {
            TileSize = tileSize;
            BlockHeight = blockHeight;
        }

        /// <summary>World units across one tile.</summary>
        public float TileSize { get; }

        /// <summary>How tall a standing block is.</summary>
        public float BlockHeight { get; }

        /// <summary>Projects a continuous grid position onto the ground plane.</summary>
        public Vector3 GridToWorld(float gridX, float gridY, float height = 0f) =>
            new Vector3(gridX * TileSize, height, gridY * TileSize);

        /// <summary>Projects the centre of a tile onto the ground plane.</summary>
        public Vector3 TileToWorld(GridCoord tile, float height = 0f) =>
            GridToWorld(tile.X, tile.Y, height);

        /// <summary>Projects an exact sub-tile position onto the ground plane.</summary>
        public Vector3 PositionToWorld(SubTilePoint position, float height = 0f) =>
            GridToWorld(ToGrid(position.X), ToGrid(position.Y), height);

        /// <summary>
        /// Converts a screen-space stick direction into grid space.
        /// </summary>
        /// <remarks>
        /// The identity, because the camera looks down the board's axes: pushing up the screen runs
        /// away from the camera, which is grid north. Should the camera ever be rotated about Y, this
        /// is the single place that has to compensate — which is exactly why input asks for the
        /// conversion instead of assuming one.
        /// </remarks>
        public Vector2 ScreenToGrid(Vector2 screenDirection) => screenDirection;

        /// <summary>The world-space footprint of a board of the given size, on the ground plane.</summary>
        public Bounds BoardBounds(int width, int height) => new Bounds(
            new Vector3((width - 1) * 0.5f * TileSize, 0f, (height - 1) * 0.5f * TileSize),
            new Vector3(width * TileSize, BlockHeight, height * TileSize));

        /// <summary>Converts a sub-tile coordinate to continuous grid space.</summary>
        public static float ToGrid(int subTileUnits) =>
            ((float)subTileUnits / SubTilePoint.UnitsPerTile) - 0.5f;
    }
}
