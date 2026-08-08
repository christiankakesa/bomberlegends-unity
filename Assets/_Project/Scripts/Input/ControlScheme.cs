namespace BomberLegends.Input
{
    /// <summary>A family of devices the game can be played with.</summary>
    public enum ControlScheme : byte
    {
        /// <summary>Keyboard for movement, mouse for aim.</summary>
        KeyboardMouse = 0,

        /// <summary>A gamepad: left stick moves, right stick aims.</summary>
        Gamepad = 1,

        /// <summary>On-screen controls.</summary>
        Touch = 2
    }
}
