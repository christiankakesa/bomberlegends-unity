namespace BomberLegends.Services
{
    /// <summary>
    /// The smallest text the interface may draw, and the arithmetic that decides it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every canvas in the project is scaled the same way — ScaleWithScreenSize against a
    /// 1920x1080 reference at match 0.5, set by SceneScaffolder.ConfigureScaler. That makes a
    /// font size a canvas unit rather than a pixel, and a canvas unit is worth very different
    /// amounts of eye depending on the screen it lands on.
    /// </para>
    /// <para>
    /// On the Galaxy S21 Ultra this project is verified against, the panel is 1440x3200 at density
    /// 600 — 3.75 physical pixels per dp — and the scaler settles at a factor of about 1.4907 in
    /// landscape. One canvas unit is therefore 1.4907 / 3.75, about 0.3975 dp. Body text needs
    /// roughly 14 dp to be read at arm's length, which puts the floor at 35.2 units.
    /// </para>
    /// <para>
    /// The device's display mode does not enter into it, which is worth stating because it looks
    /// as though it should. The S21 Ultra ships in FHD+ rather than at its panel resolution, so the
    /// app actually renders at 1080x2400 at density 450: a scale factor of 1.1180 against 2.8125
    /// pixels per dp, which is the same 0.3975 dp per unit. It has to be — the screen is 384 dp
    /// wide in both modes, and dp is what the eye is measuring. Checking only the panel resolution
    /// is how a figure of 3.22 pixels per dp came to be written down, and with it a floor 16 per
    /// cent lower than the real one.
    /// </para>
    /// <para>
    /// This lives in one place because the alternative was already tried. Every screen picked its
    /// own sizes by eye on a monitor, where they looked correct, and at the touch gate none of
    /// three testers could describe the build they were playing: the item descriptions were
    /// rendering at about 9 dp. A size that reads as pleasantly subordinate on a desktop is simply
    /// not there in a hand, and no amount of looking at it on the desktop reveals that.
    /// </para>
    /// </remarks>
    public static class TextLegibility
    {
        /// <summary>Canvas scale factor on the device the project is verified against.</summary>
        /// <remarks>Landscape, at the 1440x3200 panel resolution.</remarks>
        public const float DeviceCanvasScale = 1.4907f;

        /// <summary>Physical pixels per dp at that same resolution.</summary>
        /// <remarks>
        /// Read off the device rather than derived: <c>dumpsys display</c> reports density 600 at
        /// 1440x3200, and 450 at the 1080x2400 the phone actually ships in. Both give the ratio
        /// below once the matching scale factor is applied.
        /// </remarks>
        public const float DevicePixelsPerDp = 3.75f;

        /// <summary>Below this, body text on a phone stops being readable at arm's length.</summary>
        public const float MinimumBodyDp = 14f;

        /// <summary>
        /// The smallest font size body text may be given, in canvas units.
        /// </summary>
        /// <remarks>
        /// 36 rather than the 35.2 the arithmetic asks for, rounded up. The rounding is the whole
        /// margin there is, so nothing here should be trimmed on the grounds that it looks large
        /// in the editor — the editor is the view that got this wrong the first two times.
        /// </remarks>
        public const int MinimumBodySize = 36;

        /// <summary>What a canvas unit is worth on the verified device, in dp.</summary>
        public static float DpPerUnit => DeviceCanvasScale / DevicePixelsPerDp;

        /// <summary>How large the given font size renders on the verified device, in dp.</summary>
        public static float DpFor(int fontSize) => fontSize * DpPerUnit;
    }
}
