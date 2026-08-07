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
            bool cornerAssistEnabled,
            int fuseTicks = 90,
            int blastLingerTicks = 12,
            int bombCooldownTicks = 0,
            int startingBombCapacity = 1,
            int startingBlastRange = 2,
            int maxBombs = 16,
            int playerRadius = 340,
            int cornerSlipPerTick = 120,
            int cornerSlipTolerance = 320)
        {
            MoveSpeedPerTick = moveSpeedPerTick;
            LaneSnapPerTick = laneSnapPerTick;
            TurnTolerance = turnTolerance;
            DirectionDeadzone = directionDeadzone;
            CornerAssistEnabled = cornerAssistEnabled;
            FuseTicks = fuseTicks;
            BlastLingerTicks = blastLingerTicks;
            BombCooldownTicks = bombCooldownTicks;
            StartingBombCapacity = startingBombCapacity;
            StartingBlastRange = startingBlastRange;
            MaxBombs = maxBombs;
            PlayerRadius = playerRadius;
            CornerSlipPerTick = cornerSlipPerTick;
            CornerSlipTolerance = cornerSlipTolerance;
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

        /// <summary>Ticks a bomb burns before it goes off. Ninety is three seconds.</summary>
        public int FuseTicks { get; }

        /// <summary>How long a blast tile stays lethal after it appears.</summary>
        public int BlastLingerTicks { get; }

        /// <summary>
        /// Ticks that must pass between placements, on top of having a bomb available.
        /// </summary>
        /// <remarks>
        /// Zero gives the classic model, where the limit is how many bombs are on the board and the
        /// placement rate is the fuse. The design document specified a five-second cooldown starting
        /// at placement instead, which leaves a player with one bomb standing idle for two seconds of
        /// every cycle. This field exists so that can be measured on a device rather than debated.
        /// </remarks>
        public int BombCooldownTicks { get; }

        /// <summary>How many bombs the player may have on the board at the start of a match.</summary>
        public int StartingBombCapacity { get; }

        /// <summary>How far each arm of the player's blast reaches at the start of a match.</summary>
        public int StartingBlastRange { get; }

        /// <summary>The most bombs that can exist at once, across every source.</summary>
        public int MaxBombs { get; }

        /// <summary>
        /// Half the width of the player's collision box, in sub-tile units.
        /// </summary>
        /// <remarks>
        /// Must stay below half a tile, or the player could not fit down a one-tile corridor. Smaller
        /// values make squeezing past corners more forgiving, which matters far more with continuous
        /// movement than it did on a lane grid.
        /// </remarks>
        public int PlayerRadius { get; }

        /// <summary>
        /// How far the player is nudged sideways per tick to slip around a corner they are clipping.
        /// </summary>
        /// <remarks>
        /// Continuous movement through a grid of one-tile corridors catches on corners constantly:
        /// turning while slightly off-centre puts one corner of the player's box inside the block and
        /// stops them dead. Nudging them clear is what makes turning feel intentional rather than
        /// fought. Zero disables the assistance entirely.
        /// </remarks>
        public int CornerSlipPerTick { get; }

        /// <summary>
        /// The most a player may be clipping a corner and still be helped past it.
        /// </summary>
        /// <remarks>
        /// Beyond this they are genuinely trying to walk into a wall, and sliding them sideways would
        /// feel like the game was steering for them.
        /// </remarks>
        public int CornerSlipTolerance { get; }

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

            if (FuseTicks <= 0)
            {
                throw new ArgumentException("A fuse must last at least one tick.");
            }

            if (BlastLingerTicks <= 0)
            {
                throw new ArgumentException("A blast must be lethal for at least one tick.");
            }

            if (BombCooldownTicks < 0)
            {
                throw new ArgumentException("Bomb cooldown must not be negative.");
            }

            if (StartingBombCapacity <= 0 || StartingBombCapacity > MaxBombs)
            {
                throw new ArgumentException("Starting bomb capacity must be between one and the maximum.");
            }

            if (StartingBlastRange <= 0)
            {
                throw new ArgumentException("Blast range must be at least one tile.");
            }

            if (PlayerRadius <= 0 || PlayerRadius >= Core.SubTilePoint.HalfTile)
            {
                throw new ArgumentException(
                    "Player radius must be positive and smaller than half a tile, or the player " +
                    "could not fit down a single-tile corridor.");
            }
        }
    }
}
