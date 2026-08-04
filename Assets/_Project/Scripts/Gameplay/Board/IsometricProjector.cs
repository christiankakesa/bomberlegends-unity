using BomberLegends.Core;
using BomberLegends.Input;
using UnityEngine;

namespace BomberLegends.Gameplay.Board
{
    /// <summary>
    /// Converts between the simulation's grid space and the isometric view.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The grid is the authority: gameplay never asks where something is on screen. This type exists
    /// so exactly one place knows the projection, and so the inverse used by input is guaranteed to
    /// match the forward transform used by rendering.
    /// </para>
    /// <para>
    /// A tile projects to a diamond <see cref="TileWidth"/> across and <see cref="TileHeight"/> tall.
    /// The four grid directions therefore land on screen diagonals, which is why input needs
    /// <see cref="ScreenToGrid"/> rather than passing the stick through unchanged.
    /// </para>
    /// </remarks>
    public sealed class IsometricProjector : IGridProjection
    {
        /// <summary>
        /// Sorting-order steps per tile of depth. Gives sub-tile resolution so a moving actor sorts
        /// correctly part-way between two tiles instead of popping at the boundary.
        /// </summary>
        public const int SortingPrecision = 32;

        /// <summary>Sorting offset applied to floor tiles so they never occlude anything.</summary>
        public const int FloorSortingOffset = -5000;

        /// <summary>Creates a projector.</summary>
        /// <param name="tileWidth">World units across one tile's diamond.</param>
        /// <param name="tileHeight">World units tall. Half the width gives the classic 2:1 look.</param>
        public IsometricProjector(float tileWidth = 1f, float tileHeight = 0.5f)
        {
            TileWidth = tileWidth;
            TileHeight = tileHeight;
        }

        /// <summary>World units across one tile's diamond.</summary>
        public float TileWidth { get; }

        /// <summary>World units tall for one tile's diamond.</summary>
        public float TileHeight { get; }

        /// <summary>Projects a continuous grid position to world space.</summary>
        public Vector2 GridToWorld(float gridX, float gridY) => new Vector2(
            (gridX - gridY) * (TileWidth * 0.5f),
            (gridX + gridY) * (TileHeight * 0.5f));

        /// <summary>Projects the centre of a tile to world space.</summary>
        public Vector2 TileToWorld(GridCoord tile) => GridToWorld(tile.X, tile.Y);

        /// <summary>Projects an exact sub-tile position to world space.</summary>
        public Vector2 PositionToWorld(SubTilePoint position) => GridToWorld(
            (float)position.X / SubTilePoint.UnitsPerTile - 0.5f,
            (float)position.Y / SubTilePoint.UnitsPerTile - 0.5f);

        /// <summary>
        /// Converts a screen-space direction into grid space.
        /// </summary>
        /// <remarks>
        /// The exact inverse of <see cref="GridToWorld"/>, so pushing the stick towards a point on
        /// screen moves the player towards that point rather than somewhere 45 degrees off. Using a
        /// plain rotation instead would be subtly wrong for any tile that is not square.
        /// </remarks>
        public Vector2 ScreenToGrid(Vector2 screenDirection)
        {
            var halfWidth = TileWidth * 0.5f;
            var halfHeight = TileHeight * 0.5f;

            var a = screenDirection.x / halfWidth;
            var b = screenDirection.y / halfHeight;

            return new Vector2((a + b) * 0.5f, (b - a) * 0.5f);
        }

        /// <summary>
        /// The sorting order for something at a continuous grid position.
        /// </summary>
        /// <remarks>
        /// Depth is <c>gridX + gridY</c>: the further along both axes, the further back in the
        /// scene, so it must draw first. Derived from grid coordinates rather than left to Unity's
        /// transparency sort, which would key off world position and flicker whenever two objects
        /// tie.
        /// </remarks>
        public static int SortingOrder(float gridX, float gridY) =>
            Mathf.RoundToInt(-(gridX + gridY) * SortingPrecision);

        /// <summary>The sorting order for a floor tile, always behind actors and blocks.</summary>
        public static int FloorSortingOrder(GridCoord tile) =>
            SortingOrder(tile.X, tile.Y) + FloorSortingOffset;
    }
}
