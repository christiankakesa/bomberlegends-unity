namespace BomberLegends.Core
{
    /// <summary>
    /// A mixer bus in the project's audio hierarchy.
    /// </summary>
    /// <remarks>
    /// Lives in Core rather than Services because both the audio service (which sets bus levels)
    /// and the audio ScriptableObjects in Data (which declare their routing) need it, and Data
    /// must never depend on Services.
    /// Every sound is routed through one of these. No <c>AudioSource</c> sets its own volume:
    /// levels are controlled by exposed mixer parameters so a single change affects the whole bus.
    /// </remarks>
    public enum AudioBus : byte
    {
        /// <summary>The root bus. Controls everything.</summary>
        Master = 0,

        /// <summary>Background music.</summary>
        Music = 1,

        /// <summary>Gameplay sound effects: bombs, blasts, destruction.</summary>
        Sfx = 2,

        /// <summary>Interface sounds.</summary>
        Ui = 3,

        /// <summary>Character voice. Reserved; unused in the vertical slice.</summary>
        Voice = 4,

        /// <summary>Environmental ambience.</summary>
        Ambience = 5
    }
}
