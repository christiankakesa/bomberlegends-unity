using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Tests.PlayMode
{
    /// <summary>
    /// Builds a canvas scaled exactly the way the shipped scenes scale theirs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared by every legibility test rather than copied into each one. Two copies of this
    /// arithmetic is how the problem it measures came about in the first place: each screen
    /// decided for itself what a readable size was, and each of them was wrong in a different way.
    /// </para>
    /// <para>
    /// It reproduces <c>SceneScaffolder.ConfigureScaler</c>. Duplicated from the scaffolder rather
    /// than shared with it because the scaffolder is editor-only; if the two ever drift, the
    /// numbers these tests assert stop describing the shipped screen, which is worth a comment on
    /// both sides.
    /// </para>
    /// </remarks>
    internal static class PhoneCanvas
    {
        /// <summary>The reference the canvas scaler is configured against.</summary>
        public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        /// <summary>The device this project is verified on, in landscape.</summary>
        public static readonly Vector2 GalaxyS21Ultra = new Vector2(3200f, 1440f);

        /// <summary>Physical pixels per dp at that resolution.</summary>
        /// <remarks>
        /// Density 600, as the device reports it. The phone ships in FHD+ and so renders at
        /// 1080x2400 at density 450 instead, which works out at the same dp per canvas unit; see
        /// <see cref="TextLegibility"/>.
        /// </remarks>
        public const float GalaxyS21UltraPixelsPerDp = 3.75f;

        /// <summary>
        /// Builds a screen-space canvas at the given resolution, and reports the scale factor the
        /// real scaler would have arrived at.
        /// </summary>
        public static GameObject Build(Vector2 resolution, out float scale)
        {
            var root = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // The scaler reads the real screen, which a test cannot resize, so the factor it would
            // arrive at is computed the way CanvasScaler does and applied to the canvas directly.
            scale = Mathf.Pow(resolution.x / Reference.x, 0.5f) *
                    Mathf.Pow(resolution.y / Reference.y, 0.5f);

            scaler.enabled = false;
            canvas.scaleFactor = scale;

            root.GetComponent<RectTransform>().sizeDelta = resolution / scale;

            return root;
        }

        /// <summary>How large the given font size renders on the verified device, in dp.</summary>
        public static float DpOf(int fontSize, float scale) =>
            fontSize * scale / GalaxyS21UltraPixelsPerDp;
    }
}
