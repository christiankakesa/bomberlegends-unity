using BomberLegends.Core;
using BomberLegends.Simulation.Actors;
using BomberLegends.Simulation.Events;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Moves the player freely in any direction, colliding with the grid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The MOBA half of the hybrid. The player is a box that travels continuously; the board it
    /// collides against is still made of whole tiles, and bombs and blasts still snap to them. That
    /// split is the whole design: fluid to control, readable to survive.
    /// </para>
    /// <para>
    /// Two behaviours carry the feel:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Per-axis resolution</b> produces wall sliding. Running diagonally into a wall drops
    /// the blocked component and keeps the other, so the player slides along the surface instead of
    /// stopping dead. This is the continuous successor to the grid version's corner assist, and it
    /// is just as essential — sticking on walls is what makes a game feel cheap.</item>
    /// <item><b>Sub-stepping</b> caps how far the box travels before collision is re-checked, so no
    /// speed can carry it through a wall.</item>
    /// </list>
    /// <para>
    /// Every calculation is integer. Normalising a stick vector needs a square root, and the
    /// floating-point one is not bit-identical across platforms; one differing bit in a velocity
    /// compounds into a divergent match, which would break replays and any server-side validation.
    /// </para>
    /// </remarks>
    public static class MovementSystem
    {
        /// <summary>Advances the player by one tick.</summary>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            in PlayerIntent intent,
            SimEventBuffer events)
        {
            ref var player = ref state.Player;

            var previousTile = player.Tile;
            var previousPosition = player.Position;

            // A dash overrides steering entirely for its duration. Committing to the direction is
            // what makes it read as a dash rather than a burst of speed, and it is what stops it
            // being a strictly better way to walk: you cannot correct mid-flight.
            var dashing = player.IsDashing;
            player.DashedThisTick = dashing;

            if (dashing)
            {
                player.DashTicksRemaining--;
            }

            var velocityX = player.DashVelocityX;
            var velocityY = player.DashVelocityY;

            if (!dashing)
            {
                ComputeVelocity(intent, config, out velocityX, out velocityY);
            }

            if (velocityX != 0 || velocityY != 0)
            {
                player.MoveDirection = DominantDirection(velocityX, velocityY);
                player.Facing = player.MoveDirection;

                // Recorded before lane assist bends it, and only while steering: this is where the
                // player was actually going, which is what a dash launched a moment later should
                // follow. Facing alone would round it to the nearest of four cardinals.
                if (!dashing)
                {
                    player.LastHeadingX = velocityX;
                    player.LastHeadingY = velocityY;

                    ApplyLaneAssist(player, config, ref velocityX, ref velocityY);
                }

                // Bombs the player is already standing on cannot block them this tick. That is the
                // whole of the classic "walk off your own bomb" rule, with no ownership tracking:
                // they can leave, and once clear the bomb blocks them like any other obstacle.
                var exempt = GridMotion.OverlappedBombs(
                    player.Position, config.PlayerRadius, state.BombGrid);

                player.Position = GridMotion.Move(
                    player.Position,
                    velocityX,
                    velocityY,
                    config.PlayerRadius,
                    state.Board,
                    state.BombGrid,
                    exempt,
                    config.CornerSlipPerTick,
                    config.CornerSlipTolerance);
            }
            else
            {
                player.MoveDirection = Direction.None;
            }

            player.IsMoving = player.Position != previousPosition;

            // A dash stopped dead by a wall ends there. Letting it grind out its remaining ticks
            // would hold the player's steering hostage while they visibly go nowhere.
            if (dashing && !player.IsMoving)
            {
                player.DashTicksRemaining = 0;
            }

            var tile = player.Tile;
            if (tile != previousTile)
            {
                events.Add(new SimEvent(SimEventType.PlayerTileEntered, tile));
            }
        }

        /// <summary>
        /// Converts the stick into a velocity in sub-tile units per tick.
        /// </summary>
        /// <remarks>
        /// Magnitude is clamped to the stick's full range before scaling, so pushing diagonally is
        /// not faster than pushing straight — the classic bug in twin-stick movement. Partial
        /// deflection is preserved below that, so the stick is genuinely analogue.
        /// </remarks>
        private static void ComputeVelocity(
            in PlayerIntent intent, in SimulationConfig config, out int velocityX, out int velocityY)
        {
            int x = intent.MoveX;
            int y = intent.MoveY;

            var lengthSquared = (x * x) + (y * y);
            var deadzone = config.DirectionDeadzone;

            if (lengthSquared < deadzone * deadzone)
            {
                velocityX = 0;
                velocityY = 0;
                return;
            }

            var length = IntMath.Sqrt(lengthSquared);
            var speed = config.MoveSpeedPerTick;

            if (length <= PlayerIntent.AxisRange)
            {
                velocityX = x * speed / PlayerIntent.AxisRange;
                velocityY = y * speed / PlayerIntent.AxisRange;
                return;
            }

            velocityX = x * speed / length;
            velocityY = y * speed / length;
        }

        /// <summary>
        /// Draws the player toward the middle of the corridor they are running along.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The board is made of whole tiles but the player is a box, and the two disagree the moment
        /// the box sits off-centre: a one-tile gap leaves only a fraction of a tile of slack, so a
        /// player pressed against one side clips the corner of every pillar they pass and is stopped
        /// dead for a few ticks at each one. It reads as the world dragging on you.
        /// </para>
        /// <para>
        /// It is felt far more on a pad than on a keyboard, and that is the tell. Keys produce
        /// perfectly axis-aligned input, so a keyboard player never drifts off-lane and never pays
        /// the toll. A stick is a degree or two off almost always, and pays it at every junction.
        /// </para>
        /// <para>
        /// So the help is proportional to how axis-aligned the request is, and gone entirely well
        /// before a diagonal. Running a corridor gets centred; cutting across open ground is left
        /// completely alone, which is what keeps the movement continuous rather than railed.
        /// </para>
        /// </remarks>
        private static void ApplyLaneAssist(
            in PlayerState player, in SimulationConfig config, ref int velocityX, ref int velocityY)
        {
            var assist = config.PlayerLaneAssistPerTick;
            if (assist <= 0)
            {
                return;
            }

            var absX = IntMath.Abs(velocityX);
            var absY = IntMath.Abs(velocityY);

            var horizontal = absX >= absY;
            var major = horizontal ? absX : absY;
            var minor = horizontal ? absY : absX;

            if (major <= 0)
            {
                return;
            }

            // Full help when travelling straight, nothing once the minor axis reaches half the
            // major — around 27°, comfortably short of a deliberate diagonal.
            var weight = 100 - (minor * 200 / major);
            if (weight <= 0)
            {
                return;
            }

            var centre = SubTilePoint.CentreOf(horizontal ? player.Tile.Y : player.Tile.X);
            var current = horizontal ? player.Position.Y : player.Position.X;

            var pull = IntMath.Clamp(centre - current, -assist, assist) * weight / 100;

            if (horizontal)
            {
                velocityY += pull;
                return;
            }

            velocityX += pull;
        }

        /// <summary>The cardinal direction that best describes a velocity, for facing and animation.</summary>
        private static Direction DominantDirection(int velocityX, int velocityY)
        {
            if (IntMath.Abs(velocityX) >= IntMath.Abs(velocityY))
            {
                return velocityX > 0 ? Direction.East : velocityX < 0 ? Direction.West : Direction.None;
            }

            return velocityY > 0 ? Direction.North : Direction.South;
        }

    }
}
