using UnityEngine;

namespace BomberLegends.Gameplay.Board
{
    /// <summary>
    /// Generates the flat shapes the vertical slice is played with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The slice deliberately ships with no authored art: pixel art is the most expensive asset in
    /// the project and must not be produced against mechanics that have not been validated. Drawing
    /// these procedurally also keeps the repository free of binary assets while the feel work is in
    /// progress.
    /// </para>
    /// <para>
    /// Every shape is white and tinted per renderer, so the whole board draws from a handful of
    /// textures. Replaced wholesale by a real sprite atlas at Milestone 8.
    /// </para>
    /// </remarks>
    public static class PlaceholderArt
    {
        private const int TileSize = 64;
        private const int TileBorder = 3;
        private const int DiscSize = 48;

        private static Sprite? _tile;
        private static Sprite? _disc;

        /// <summary>A square tile with an inset border, so the grid reads while moving.</summary>
        public static Sprite Tile => _tile ??= CreateTile();

        /// <summary>A filled circle, used for actors.</summary>
        public static Sprite Disc => _disc ??= CreateDisc();

        private static Sprite CreateTile()
        {
            var pixels = new Color32[TileSize * TileSize];

            for (var y = 0; y < TileSize; y++)
            {
                for (var x = 0; x < TileSize; x++)
                {
                    var onEdge = x < TileBorder || y < TileBorder ||
                                 x >= TileSize - TileBorder || y >= TileSize - TileBorder;

                    // The darker rim is what separates one tile from the next without needing a
                    // second sprite or a second draw call.
                    pixels[(y * TileSize) + x] = onEdge
                        ? new Color32(255, 255, 255, 90)
                        : new Color32(255, 255, 255, 255);
                }
            }

            return CreateSprite(pixels, TileSize, TileSize, TileSize, "PlaceholderTile");
        }

        private static Sprite CreateDisc()
        {
            var pixels = new Color32[DiscSize * DiscSize];
            var half = DiscSize * 0.5f;

            for (var y = 0; y < DiscSize; y++)
            {
                for (var x = 0; x < DiscSize; x++)
                {
                    var dx = (x + 0.5f - half) / half;
                    var dy = (y + 0.5f - half) / half;
                    var inside = (dx * dx) + (dy * dy) <= 1f;
                    pixels[(y * DiscSize) + x] = inside ? new Color32(255, 255, 255, 255) : default;
                }
            }

            return CreateSprite(pixels, DiscSize, DiscSize, DiscSize, "PlaceholderDisc");
        }

        private static Sprite CreateSprite(
            Color32[] pixels, int width, int height, int pixelsPerUnit, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,

                // Never written to disk; these exist only for the lifetime of the process.
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);

            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
