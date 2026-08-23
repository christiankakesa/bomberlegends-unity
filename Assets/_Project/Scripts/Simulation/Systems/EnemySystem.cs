using BomberLegends.Core;
using BomberLegends.Simulation.Events;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Drives the enemies: pursue the player, and collide with the world exactly as the player does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The chase is deliberately simple. It picks whichever open direction closes the distance and
    /// commits to it until the tile changes or something blocks the way. That reads as pursuit
    /// without pathfinding, and — more importantly — it is trivially deterministic, which a
    /// full-fledged planner would not be.
    /// </para>
    /// <para>
    /// Ties are broken with the match's own random source rather than a fixed preference, so two
    /// enemies in the same situation do not move as one, and a replay still reproduces exactly.
    /// </para>
    /// <para>
    /// They also <b>run from bombs about to go off</b>, which they did not before: pursuit was the
    /// only thing they could see, so they walked into explosions and roughly four kills in five
    /// came from a bomb the player never had to aim. Fear is read from
    /// <see cref="Board.ThreatGrid"/>, which answers both questions a frightened enemy has —
    /// whether this tile is about to burn, and which way is out — in one array read.
    /// </para>
    /// <para>
    /// <b>Only alerted enemies are afraid.</b> A Sentinel that has not noticed the player has no
    /// reason to understand what a bomb is, and bombing something that has not seen you coming is
    /// the whole reward for approaching an arena carefully.
    /// </para>
    /// <para>
    /// Sentinels are <b>dormant until approached</b>. Every enemy hunting from the first tick meant
    /// an arena could only ever be fought as one mass, which is what made the second sector
    /// unplayable in testing. Waking on proximity turns the same enemies into a sequence of
    /// encounters the player chooses to start, and makes the maze worth reading.
    /// </para>
    /// </remarks>
    public static class EnemySystem
    {
        /// <summary>Advances every living enemy by one tick.</summary>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            SimEventBuffer events)
        {
            var target = state.Player.Position;

            for (var slot = 0; slot < state.Enemies.Capacity; slot++)
            {
                var enemy = state.Enemies[slot];
                if (!enemy.IsActive)
                {
                    continue;
                }

                enemy.Health.Age();

                // Waking is checked before anything else moves, so a Sentinel the player has just
                // walked up to reacts on the same tick rather than a tick late.
                if (!enemy.IsAlerted)
                {
                    if (enemy.Tile.ManhattanDistanceTo(state.Player.Tile) > config.EnemyAggroRadius)
                    {
                        // Dormant: still takes damage and still blocks, but does not hunt.
                        state.Enemies[slot] = enemy;
                        continue;
                    }

                    enemy.IsAlerted = true;
                }

                var beforeTile = enemy.Tile;
                var beforePosition = enemy.Position;

                // Re-decide on arriving somewhere new, or when the current heading is exhausted.
                if (enemy.MoveDirection == Direction.None || !CanContinue(ref state, enemy))
                {
                    enemy.MoveDirection = ChooseDirection(ref state, enemy, target);
                }

                if (enemy.MoveDirection != Direction.None)
                {
                    var offset = enemy.MoveDirection.ToOffset();
                    var speed = config.EnemySpeedPerTick;

                    CentreInLane(enemy, offset, config, out var driftX, out var driftY);

                    var exempt = GridMotion.OverlappedBombs(
                        enemy.Position, config.EnemyRadius, state.BombGrid);

                    enemy.Position = GridMotion.Move(
                        enemy.Position,
                        (offset.X * speed) + driftX,
                        (offset.Y * speed) + driftY,
                        config.EnemyRadius,
                        state.Board,
                        state.BombGrid,
                        exempt,
                        config.CornerSlipPerTick,
                        config.CornerSlipTolerance);
                }

                // Stuck against something despite having a heading: remember what failed, so the
                // next decision does not simply choose it again and wedge here permanently.
                if (enemy.Position == beforePosition)
                {
                    enemy.BlockedDirection = enemy.MoveDirection;
                    enemy.MoveDirection = Direction.None;
                }
                else
                {
                    enemy.BlockedDirection = Direction.None;
                }

                if (enemy.Tile != beforeTile)
                {
                    enemy.MoveDirection = Direction.None;
                    enemy.BlockedDirection = Direction.None;
                }

                state.Enemies[slot] = enemy;
            }
        }

        /// <summary>
        /// Pulls the enemy toward the middle of the lane it is travelling along.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The chase reasons in whole tiles but the body is a box, and those two disagree the moment
        /// an enemy sits off-centre: the tile ahead reads walkable while the box is clipping the
        /// pillar beside it. Corner slip, which exists to help the <i>player</i> round corners, is
        /// what knocks them off-centre in the first place — so the assist was quietly creating the
        /// condition that trapped them.
        /// </para>
        /// <para>
        /// Centred enemies never clip a corner, so the disagreement cannot arise. This is what
        /// <see cref="SimulationConfig.LaneSnapPerTick"/> was written for; it went unused when the
        /// player moved to free 360° travel, and it is exactly right here.
        /// </para>
        /// </remarks>
        private static void CentreInLane(
            in Actors.EnemyState enemy,
            GridCoord offset,
            in SimulationConfig config,
            out int driftX,
            out int driftY)
        {
            driftX = 0;
            driftY = 0;

            var snap = config.LaneSnapPerTick;
            if (snap <= 0)
            {
                return;
            }

            if (offset.X != 0)
            {
                var centre = SubTilePoint.CentreOf(enemy.Tile.Y);
                driftY = IntMath.Clamp(centre - enemy.Position.Y, -snap, snap);
                return;
            }

            if (offset.Y != 0)
            {
                var centre = SubTilePoint.CentreOf(enemy.Tile.X);
                driftX = IntMath.Clamp(centre - enemy.Position.X, -snap, snap);
            }
        }

        /// <summary>Whether the enemy's current heading is still open, and still safe.</summary>
        /// <remarks>
        /// Committing to a heading until the tile changes is an optimisation, and a bomb laid since
        /// that decision was taken invalidates it. Without this check an enemy would carry on into
        /// fire on momentum alone, which is the free kill this system exists to remove — and it
        /// would look far worse than obliviousness, because it would happen with a way out in
        /// sight.
        /// </remarks>
        private static bool CanContinue(ref SimulationState state, in Actors.EnemyState enemy)
        {
            var ahead = enemy.Tile.Neighbour(enemy.MoveDirection);

            if (!state.Board.IsWalkable(ahead) || state.BombGrid.HasBomb(ahead))
            {
                return false;
            }

            return state.Threats.EscapeStepsFrom(ahead) <=
                   state.Threats.EscapeStepsFrom(enemy.Tile);
        }

        /// <summary>
        /// Picks a heading: away from fire first, towards the player second, and nowhere at all
        /// when fire is the only thing in the way.
        /// </summary>
        private static Direction ChooseDirection(
            ref SimulationState state, in Actors.EnemyState enemy, SubTilePoint target)
        {
            var choice = Appraise(ref state, enemy, target, enemy.BlockedDirection);

            if (choice.Direction == Direction.None)
            {
                // Everything else was walled off, so the failed heading is all that is left. Trying
                // it again beats standing still: whatever was clipping may since have moved.
                choice = Appraise(ref state, enemy, target, Direction.None);
            }

            var here = state.Threats.EscapeStepsFrom(enemy.Tile);

            // Never step into more danger than you are already standing in. For an enemy caught
            // inside a blast this is no restriction at all — every step on its way out scores
            // better than the tile it is on — but for one at the edge it is absolute.
            if (choice.EscapeSteps > here)
            {
                return Direction.None;
            }

            // Clear of the fire, and the way the chase wants to go is burning. Hold, rather than
            // take the long way round: a blast is a wall that will not be there in a moment, and
            // giving up ground to it costs more than waiting does. This is also what lets a player
            // use a bomb as a wall, which is the oldest idea in the genre.
            if (here == Board.ThreatGrid.Safe && choice.ShortestDistance < choice.Distance)
            {
                return Direction.None;
            }

            return choice.Direction;
        }

        /// <summary>
        /// Scores the four ways out of a tile.
        /// </summary>
        /// <remarks>
        /// Safety outranks pursuit outright rather than being weighed against it: an enemy that
        /// traded a step towards the player against a step away from an explosion would sometimes
        /// take the trade, and no amount of pursuit is worth dying for. With nothing burning every
        /// step scores zero for safety and this decides on distance alone — the same chase, making
        /// the same rolls, as before enemies knew what a bomb was.
        /// </remarks>
        private static Choice Appraise(
            ref SimulationState state,
            in Actors.EnemyState enemy,
            SubTilePoint target,
            Direction avoid)
        {
            var from = enemy.Tile;
            var toward = target.Tile;

            var best = Direction.None;
            var bestSteps = int.MaxValue;
            var bestDistance = int.MaxValue;
            var shortest = int.MaxValue;
            var tied = 0;

            var cardinals = Directions.Cardinals;
            for (var i = 0; i < cardinals.Length; i++)
            {
                var candidate = cardinals[i];

                if (candidate == avoid)
                {
                    continue;
                }

                var step = from.Neighbour(candidate);

                if (!state.Board.IsWalkable(step) || state.BombGrid.HasBomb(step))
                {
                    continue;
                }

                var steps = state.Threats.EscapeStepsFrom(step);
                var distance = step.ManhattanDistanceTo(toward);

                // Tracked whether the step is safe or not: it is how the caller tells "the chase is
                // satisfied" apart from "the chase is being turned aside by fire".
                if (distance < shortest)
                {
                    shortest = distance;
                }

                if (steps < bestSteps || (steps == bestSteps && distance < bestDistance))
                {
                    best = candidate;
                    bestSteps = steps;
                    bestDistance = distance;
                    tied = 1;
                    continue;
                }

                if (steps != bestSteps || distance != bestDistance)
                {
                    continue;
                }

                // Reservoir sampling over equally good options, so two enemies in identical
                // situations do not march in lockstep — and a replay still reproduces exactly.
                tied++;
                if (state.Random.NextInt(tied) == 0)
                {
                    best = candidate;
                }
            }

            return new Choice(best, bestSteps, bestDistance, shortest);
        }

        /// <summary>One tick's worth of thinking about where to go.</summary>
        private readonly struct Choice
        {
            public Choice(Direction direction, int escapeSteps, int distance, int shortestDistance)
            {
                Direction = direction;
                EscapeSteps = escapeSteps;
                Distance = distance;
                ShortestDistance = shortestDistance;
            }

            /// <summary>The way to go, or <see cref="Direction.None"/> when nothing was open.</summary>
            public Direction Direction { get; }

            /// <summary>How far that step is from safety; zero when it is clear of every blast.</summary>
            public int EscapeSteps { get; }

            /// <summary>How far that step leaves the enemy from the player.</summary>
            public int Distance { get; }

            /// <summary>
            /// The closest any open step gets to the player, fire disregarded.
            /// </summary>
            /// <remarks>
            /// Equal to <see cref="Distance"/> whenever the chase got what it wanted. Smaller means
            /// the step the chase would have taken is on fire, and the enemy is being turned aside
            /// by something temporary.
            /// </remarks>
            public int ShortestDistance { get; }
        }
    }
}