using BomberLegends.Core;

namespace BomberLegends.Simulation
{
    /// <summary>
    /// One tick of what the player is asking for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The complete input surface of the game, in three bytes. A match is fully reproducible from
    /// its seed, its layout and the sequence of these — which is what makes replays, the determinism
    /// tests and any future networked play possible without touching gameplay code.
    /// </para>
    /// <para>
    /// The stick is quantised to whole numbers deliberately. Floats sampled from a touch surface
    /// differ in their low bits between devices, and that difference would compound into divergent
    /// simulations.
    /// </para>
    /// </remarks>
    public readonly struct PlayerIntent
    {
        /// <summary>Largest magnitude either axis can carry.</summary>
        public const int AxisRange = 100;

        /// <summary>Below this magnitude on both axes, no direction is requested.</summary>
        public const int DefaultDeadzone = 30;

        /// <summary>Horizontal stick position, from -100 to 100.</summary>
        public readonly sbyte MoveX;

        /// <summary>Vertical stick position, from -100 to 100.</summary>
        public readonly sbyte MoveY;

        /// <summary>Horizontal aim, from -100 to 100.</summary>
        /// <remarks>
        /// Separate from movement because skillshots are aimed independently of where the player is
        /// running. Added to the intent now rather than later: this struct is the replay format and
        /// the future network packet, so widening it costs nothing today and invalidates every
        /// recorded run once there are any.
        /// </remarks>
        public readonly sbyte AimX;

        /// <summary>Vertical aim, from -100 to 100.</summary>
        public readonly sbyte AimY;

        /// <summary>Action buttons held this tick.</summary>
        public readonly IntentButtons Buttons;

        /// <summary>Creates an intent.</summary>
        public PlayerIntent(
            sbyte moveX,
            sbyte moveY,
            IntentButtons buttons = IntentButtons.None,
            sbyte aimX = 0,
            sbyte aimY = 0)
        {
            MoveX = moveX;
            MoveY = moveY;
            Buttons = buttons;
            AimX = aimX;
            AimY = aimY;
        }

        /// <summary>Whether an aim direction is being supplied this tick.</summary>
        public bool HasAim => AimX != 0 || AimY != 0;

        /// <summary>No movement and no buttons.</summary>
        public static PlayerIntent None => default;

        /// <summary>Creates an intent pushing fully in one cardinal direction.</summary>
        public static PlayerIntent FromDirection(Direction direction, IntentButtons buttons = IntentButtons.None)
        {
            var offset = direction.ToOffset();
            return new PlayerIntent((sbyte)(offset.X * AxisRange), (sbyte)(offset.Y * AxisRange), buttons);
        }

        /// <summary>
        /// The cardinal direction being requested, or <see cref="Direction.None"/> inside the
        /// deadzone or on an exact diagonal.
        /// </summary>
        /// <remarks>
        /// The dominant axis wins; a dead-exact diagonal requests nothing rather than picking an
        /// axis arbitrarily. Smoothing the raw stick so it rarely sits on a diagonal is the input
        /// layer's job, not the simulation's.
        /// </remarks>
        public Direction ToDirection(int deadzone = DefaultDeadzone)
        {
            int absX = MoveX < 0 ? -MoveX : MoveX;
            int absY = MoveY < 0 ? -MoveY : MoveY;

            if (absX < deadzone && absY < deadzone)
            {
                return Direction.None;
            }

            if (absX == absY)
            {
                return Direction.None;
            }

            if (absX > absY)
            {
                return MoveX > 0 ? Direction.East : Direction.West;
            }

            return MoveY > 0 ? Direction.North : Direction.South;
        }
    }
}
