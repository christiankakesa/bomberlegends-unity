using UnityEngine;

namespace BomberLegends.Data.Balance
{
    /// <summary>
    /// The handful of numbers that decide whether the controls feel tight or sticky.
    /// </summary>
    /// <remarks>
    /// Isolated in one asset on purpose: these values cannot be reasoned into correctness, they have
    /// to be tuned against real thumbs on a real device, and doing that means changing them while
    /// the game is running and feeling the difference immediately.
    /// </remarks>
    [CreateAssetMenu(menuName = "Bomber Legends/Balance/Input Feel", fileName = "InputFeel")]
    public sealed class InputFeelConfig : ScriptableObject
    {
        [Header("Stick")]
        [SerializeField, Range(0.01f, 0.9f)]
        [Tooltip("Stick displacement below which no direction is requested. Too low and a resting " +
                 "thumb drifts; too high and small corrections are ignored.")]
        private float _deadzone = 0.25f;

        [SerializeField, Range(1f, 3f)]
        [Tooltip("How much larger a new axis must be before the direction changes. One means the " +
                 "character flickers between directions whenever the thumb sits near a diagonal.")]
        private float _switchRatio = 1.4f;

        [Header("Buffering")]
        [SerializeField, Range(0f, 0.4f)]
        [Tooltip("How long a direction change survives the stick returning to centre, so a quick " +
                 "flick still registers. Only applies to changes, so a deliberate release still " +
                 "stops the player immediately.")]
        private float _changeBufferSeconds = 0.12f;

        /// <summary>Stick displacement below which no direction is requested.</summary>
        public float Deadzone => _deadzone;

        /// <summary>How much larger a new axis must be before the direction changes.</summary>
        public float SwitchRatio => _switchRatio;

        /// <summary>How long a direction change survives the stick returning to centre.</summary>
        public float ChangeBufferSeconds => _changeBufferSeconds;
    }
}
