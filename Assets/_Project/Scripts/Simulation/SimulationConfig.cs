using System;

namespace BomberLegends.Simulation
{
    /// <summary>
    /// Tuning the simulation reads once at construction.
    /// </summary>
    /// <remarks>
    /// Plain data with no engine types, baked from authoring assets by the Data layer. Distances are
    /// sub-tile units per tick, so every value here is exact integer arithmetic.
    /// </remarks>
    public readonly struct SimulationConfig
    {
        /// <summary>Creates a configuration.</summary>
        public SimulationConfig(
            int moveSpeedPerTick,
            int laneSnapPerTick,
            int turnTolerance,
            int directionDeadzone,
            bool cornerAssistEnabled)
        {
            MoveSpeedPerTick = moveSpeedPerTick;
            LaneSnapPerTick = laneSnapPerTick;
            TurnTolerance = turnTolerance;
            DirectionDeadzone = directionDeadzone;
            CornerAssistEnabled = cornerAssistEnabled;
        }

        /// <summary>Sub-tile units the player advances each tick while moving.</summary>
        public int MoveSpeedPerTick { get; }

        /// <summary>
        /// Sub-tile units the player is pulled towards the centre of their lane each tick while
        /// moving. This is what stops a player drifting along the edge of a corridor.
        /// </summary>
        public int LaneSnapPerTick { get; }

        /// <summary>
        /// How far off the centre of a lane the player may be and still be allowed to turn into it.
        /// Larger values make turns forgiving; too large and turns feel like they teleport.
        /// </summary>
        public int TurnTolerance { get; }

        /// <summary>Stick magnitude below which no direction is requested.</summary>
        public int DirectionDeadzone { get; }

        /// <summary>
        /// Whether a blocked player may turn regardless of alignment. This is what stops a player
        /// wedging themselves in a corner, which is the single most common complaint about
        /// grid movement done badly.
        /// </summary>
        public bool CornerAssistEnabled { get; }

        /// <summary>Starting values for the vertical slice, tuned on device during T-015.</summary>
        public static SimulationConfig Default => FromTilesPerSecond(4f);

        /// <summary>Builds a configuration from a speed expressed in tiles per second.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The speed is not positive.</exception>
        public static SimulationConfig FromTilesPerSecond(
            float tilesPerSecond,
            int ticksPerSecond = SimulationConstants.TicksPerSecond)
        {
            if (tilesPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tilesPerSecond), tilesPerSecond, "Movement speed must be positive.");
            }

            if (ticksPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ticksPerSecond), ticksPerSecond, "Tick rate must be positive.");
            }

            var perTick = (int)Math.Round(
                tilesPerSecond * Core.SubTilePoint.UnitsPerTile / ticksPerSecond,
                MidpointRounding.AwayFromZero);

            return new SimulationConfig(
                moveSpeedPerTick: Math.Max(1, perTick),
                laneSnapPerTick: Math.Max(1, perTick * 3 / 2),
                turnTolerance: Core.SubTilePoint.UnitsPerTile * 3 / 10,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true);
        }

        /// <summary>Throws if any value would produce broken movement.</summary>
        /// <exception cref="ArgumentException">A value is outside its usable range.</exception>
        public void Validate()
        {
            if (MoveSpeedPerTick <= 0)
            {
                throw new ArgumentException("Movement speed must be positive.");
            }

            if (LaneSnapPerTick < 0)
            {
                throw new ArgumentException("Lane snap must not be negative.");
            }

            if (TurnTolerance < 0 || TurnTolerance > Core.SubTilePoint.HalfTile)
            {
                throw new ArgumentException(
                    "Turn tolerance must be between zero and half a tile; beyond that a turn would " +
                    "snap the player into a different tile.");
            }

            if (DirectionDeadzone < 0 || DirectionDeadzone > PlayerIntent.AxisRange)
            {
                throw new ArgumentException("Direction deadzone must be within the stick range.");
            }
        }
    }
}
