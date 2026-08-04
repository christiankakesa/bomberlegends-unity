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
        private const int DiamondWidth = 64;
        private const int DiamondHeight = 32;
        private const int DiscSize = 48;

        private static Sprite? _diamond;
        private static Sprite? _disc;

        /// <summary>A 2:1 diamond matching one projected tile.</summary>
        public static Sprite Diamond => _diamond ??= CreateDiamond();

        /// <summary>A filled circle, used for actors.</summary>
        public static Sprite Disc => _disc ??= CreateDisc();

        private static Sprite CreateDiamond()
        {
            var pixels = new Color32[DiamondWidth * DiamondHeight];
            var halfWidth = DiamondWidth * 0.5f;
            var halfHeight = DiamondHeight * 0.5f;

            for (var y = 0; y < DiamondHeight; y++)
            {
                for (var x = 0; x < DiamondWidth; x++)
                {
                    var dx = Mathf.Abs(x + 0.5f - halfWidth) / halfWidth;
                    var dy = Mathf.Abs(y + 0.5f - halfHeight) / halfHeight;
                    var inside = dx + dy <= 1f;
                    pixels[(y * DiamondWidth) + x] = inside ? new Color32(255, 255, 255, 255) : default;
                }
            }

            return CreateSprite(pixels, DiamondWidth, DiamondHeight, DiamondWidth, "PlaceholderDiamond");
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
