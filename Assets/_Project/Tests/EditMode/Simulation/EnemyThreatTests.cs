using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers enemies knowing what a bomb is: running from one, holding at the edge of one, and
    /// dying to one when the way out has been cut off.
    /// </summary>
    /// <remarks>
    /// Playtesters reported roughly four kills in five coming from bombs while the aimed shot felt
    /// useless. The bomb was not winning fights so much as winning them unattended, because nothing
    /// on the board understood what one was. These tests are about the exchange that replaces it:
    /// the enemy comes, the bomb goes down, the enemy runs, and whether it lives depends on whether
    /// the player closed the exit.
    /// </remarks>
    public sealed class EnemyThreatTests
    {
        private const int Fuse = 90;
        private const int Linger = 12;
        private const int Fear = 45;

        private static SimulationConfig Config(
            int range = 2,
            int fuse = Fuse,
            int fear = Fear,
            int aggro = 7,
            int capacity = 1) =>
            new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true,
                fuseTicks: fuse,
                blastLingerTicks: Linger,
                bombCooldownTicks: 0,
                startingBombCapacity: capacity,
                startingBlastRange: range,
                maxBombs: 16,
                playerRadius: 340,
                cornerSlipPerTick: 90,
                cornerSlipTolerance: 320,
                playerMaxHealth: 100,
                // The player is made unkillable so every test runs its full length. A finished match
                // freezes every system, and an enemy that stopped because the player died proves
                // nothing about what the enemy understood.
                blastDamageToPlayer: 0,
                enemyContactDamage: 0,
                invulnerabilityTicks: 30,
                enemyMaxHealth: 100,
                blastDamageToEnemy: 100,
                enemySpeedPerTick: 80,
                enemyRadius: 320,
                maxEnemies: 32,
                enemyAggroRadius: aggro,
                enemyBombFearTicks: fear);

        private static PlayerIntent Bomb => new PlayerIntent(0, 0, IntentButtons.Bomb);

        private static PlayerIntent Idle => PlayerIntent.None;

        private static void Advance(GameSimulation simulation, int ticks, PlayerIntent? intent = null)
        {
            var value = intent ?? Idle;
            for (var i = 0; i < ticks; i++)
            {
                simulation.Tick(value);
            }
        }

        /// <summary>A straight corridor with the enemy inside a range-two blast from the player.</summary>
        private static GameSimulation Corridor(SimulationConfig config) =>
            new GameSimulation(
                config,
                LevelLayout.Parse(
                    "###########",
                    "#P.E......#",
                    "###########"),
                seed: 1u);

        // ---------- running ----------

        [Test]
        public void AnEnemyCaughtInsideABlastRunsOutOfIt()
        {
            var simulation = Corridor(Config());

            simulation.Tick(Bomb);
            Advance(simulation, Fuse - 2);

            Assert.That(simulation.State.Enemies[0].Tile.X, Is.GreaterThanOrEqualTo(4),
                "it must leave the blast, not merely survive it");

            Advance(simulation, Linger + 5);

            Assert.That(simulation.State.Enemies.AliveCount, Is.EqualTo(1));
        }

        [Test]
        public void WithoutFearTheSameBombKillsItWhereItStands()
        {
            // The counterfactual. Zero fear is the old behaviour, and it must still be reachable —
            // a mob that does not understand bombs is a legitimate archetype, just not the only one.
            var simulation = Corridor(Config(fear: 0));

            simulation.Tick(Bomb);
            Advance(simulation, Fuse + Linger + 5);

            Assert.That(simulation.State.Enemies.AliveCount, Is.Zero,
                "the free kill must still be reproducible, or the test above proves nothing");
        }

        [Test]
        public void AnEnemyWithNoWayOutStillDies()
        {
            // The whole point. Bombs must keep killing — they must just stop killing unattended.
            // Here the blast fills the corridor end to end, so there is nowhere to run to and the
            // player has earned the kill by choosing the spot.
            var simulation = new GameSimulation(
                Config(range: 3),
                LevelLayout.Parse(
                    "######",
                    "#P.E.#",
                    "######"),
                seed: 1u);

            simulation.Tick(Bomb);
            Advance(simulation, Fuse + Linger + 5);

            Assert.That(simulation.State.Enemies.AliveCount, Is.Zero,
                "cutting off the exit is the play; it must still work");
        }

        // ---------- holding ----------

        [Test]
        public void AnEnemyHoldsAtTheEdgeOfABlastRatherThanGivingUpGround()
        {
            // A fuse as short as the fear window, so the corridor is dangerous the moment the bomb
            // lands and the enemy never gets to walk in unaware.
            var simulation = new GameSimulation(
                Config(fuse: Fear),
                LevelLayout.Parse(
                    "##########",
                    "#P..E....#",
                    "##########"),
                seed: 1u);

            simulation.Tick(Bomb);
            Advance(simulation, 30);

            Assume.That(simulation.State.Enemies[0].IsAlerted, Is.True);
            Assert.That(simulation.State.Enemies[0].Tile, Is.EqualTo(new GridCoord(4, 1)),
                "it must hold at the edge of the fire, neither walking in nor backing away from it");

            Advance(simulation, Linger + 60);

            Assert.That(simulation.State.Enemies[0].Tile.X, Is.LessThan(4),
                "and it must come on again once the fire is out — waiting is not freezing");
        }

        [Test]
        public void ADormantSentinelHasNoIdeaWhatABombIs()
        {
            // Deliberate: bombing something that has not noticed you is the reward for approaching
            // an arena carefully, and a Sentinel that flinched at a bomb it never saw would take
            // that away.
            var simulation = new GameSimulation(
                Config(aggro: 1),
                LevelLayout.Parse(
                    "#######",
                    "#P.E..#",
                    "#######"),
                seed: 1u);

            var start = simulation.State.Enemies[0].Position;

            simulation.Tick(Bomb);
            Advance(simulation, Fuse - 2);

            Assume.That(simulation.State.Enemies[0].IsAlerted, Is.False);
            Assert.That(simulation.State.Enemies[0].Position, Is.EqualTo(start),
                "a Sentinel that has not seen the player must not react to the player's bomb");

            Advance(simulation, Linger + 5);

            Assert.That(simulation.State.Enemies.AliveCount, Is.Zero);
        }

        // ---------- what is feared ----------

        [Test]
        public void WhatEnemiesFearIsExactlyWhatTheBlastWillReach()
        {
            // The guard on the one duplication in the design: the threat projection walks the same
            // arms as the blast, but cannot share its code, because the blast also destroys and
            // ignites as it goes. An enemy that feared the wrong tiles would be worse than one that
            // feared nothing, so the agreement is proved rather than asserted in a comment.
            //
            // The board is shaped to exercise every stopping rule at once: north into a destructible
            // block, south into permanent structure, east and west into open floor.
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#######",
                    "#..X..#",
                    "#..P..#",
                    "#..#..#",
                    "#######"),
                seed: 1u);

            simulation.Tick(Bomb);

            while (simulation.State.Bombs[0].FuseTicksRemaining > Fear)
            {
                simulation.Tick(Idle);
            }

            var predicted = Tiles(simulation, threatened: true);

            while (simulation.State.Bombs[0].IsActive)
            {
                simulation.Tick(Idle);
            }

            var burned = Tiles(simulation, threatened: false);

            Assert.That(burned, Is.Not.Empty, "the bomb must actually have gone off");
            Assert.That(predicted, Is.EqualTo(burned),
                "enemies must fear precisely the tiles the blast is going to reach");
        }

        [Test]
        public void FireStillBurningIsFearedJustAsAFuseAboutToEndIs()
        {
            // Fire outlives the bomb that made it by a fraction of a second. An enemy walking into
            // the embers would hand back the free kill this whole system exists to remove.
            var simulation = Corridor(Config());

            simulation.Tick(Bomb);

            while (simulation.State.Bombs[0].IsActive)
            {
                simulation.Tick(Idle);
            }

            var burning = Tiles(simulation, threatened: false);

            Assume.That(burning, Is.Not.Empty);
            Assert.That(Tiles(simulation, threatened: true), Is.SupersetOf(burning));
        }

        [Test]
        public void ABombAChainWillSetOffIsFearedWhateverItsOwnFuseSays()
        {
            // Laying a chain is a real play, and enemies have to respect the whole of it. The second
            // bomb here is nowhere near going off by its own fuse; it is going off because the first
            // one reaches it.
            const int fear = 20;

            var simulation = new GameSimulation(
                Config(fear: fear, capacity: 2),
                LevelLayout.Parse(
                    "##########",
                    "#P.......#",
                    "##########"),
                seed: 1u);

            // Only the second bomb's blast reaches this far.
            var beyond = new GridCoord(4, 1);

            simulation.Tick(Bomb);

            for (var i = 0; i < 60 && simulation.State.Player.Tile.X < 2; i++)
            {
                simulation.Tick(PlayerIntent.FromDirection(Direction.East));
            }

            simulation.Tick(Bomb);

            Assume.That(simulation.State.Bombs.ActiveCount, Is.EqualTo(2));
            Assert.That(simulation.State.Threats.IsThreatened(beyond), Is.False,
                "neither fuse is close enough to its end to be worth fearing yet");

            while (simulation.State.Bombs[0].FuseTicksRemaining > fear)
            {
                simulation.Tick(Idle);
            }

            Assume.That(simulation.State.Bombs[1].FuseTicksRemaining, Is.GreaterThan(fear),
                "the second bomb must still be far from its own detonation, or this tests nothing");
            Assert.That(simulation.State.Threats.IsThreatened(beyond), Is.True,
                "a bomb the chain will set off must be feared along with the one that sets it off");
        }

        // ---------- housekeeping ----------

        [Test]
        public void ProjectingThreats_AllocatesNothing()
        {
            // Runs without enemies on purpose: nothing can die, so the match never ends and the
            // sweep is exercised on every one of these ticks rather than on the handful before a
            // victory freezes the world.
            var simulation = new GameSimulation(
                Config(capacity: 4),
                LevelLayout.Parse(
                    "###############",
                    "#P............#",
                    "#.#.#.#.#.#.#.#",
                    "#.............#",
                    "#.#.#.#.#.#.#.#",
                    "#.............#",
                    "###############"),
                seed: 9u);

            PlayerIntent Walking(int tick) => PlayerIntent.FromDirection(
                Directions.Cardinals[(tick / 23) % 4],
                tick % 11 == 0 ? IntentButtons.Bomb : IntentButtons.None);

            for (var i = 0; i < 200; i++)
            {
                simulation.Tick(Walking(i));
            }

            var before = System.GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 5000; i++)
            {
                simulation.Tick(Walking(i));
            }

            Assert.That(System.GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero,
                "the threat sweep runs thirty times a second and must never produce garbage");
        }


        // ---------- what the arena itself decides ----------

        [Test]
        public void TheGeneratorMustNotMakeMoreOfTheArenaInescapable()
        {
            // Fear only helps an enemy that has somewhere to go, and in a generated arena that is
            // often not the case: at fifty-five percent destructible fill, two bomb placements in
            // five cover a pocket of floor with no walkable tile outside it. The blocks chop the
            // maze into segments shorter than a blast, so the kill was decided by the level, not by
            // the player — which is a large part of why bombs were reported as doing four kills in
            // five while the shot felt pointless.
            //
            // Measured rather than argued: 41% at the shipping density, 26% at 45, 14% at 35 and 6%
            // at 25. Lowering the fill is a design decision and belongs with the level generation
            // work, not here. What belongs here is a floor under it — this number must not grow.
            var current = SealedPlacementPercent(ArenaSettings.Default, range: 2, seeds: 20);

            Assert.That(current, Is.LessThanOrEqualTo(41),
                $"{current}% of bomb placements now leave nothing an enemy can do about it, which " +
                "is worse than when enemies were taught to run; the generator has grown denser");
        }

        /// <summary>
        /// How often, in percent, a bomb covers a pocket of floor with no walkable tile outside it.
        /// </summary>
        private static int SealedPlacementPercent(ArenaSettings settings, int range, uint seeds)
        {
            var placements = 0;
            var sealedOff = 0;

            for (var seed = 1u; seed <= seeds; seed++)
            {
                var random = new DeterministicRandom(seed);
                var board = ArenaGenerator.Generate(0, settings, ref random).CreateBoard();

                for (var y = 0; y < board.Height; y++)
                {
                    for (var x = 0; x < board.Width; x++)
                    {
                        var tile = new GridCoord(x, y);
                        if (!board.IsWalkable(tile))
                        {
                            continue;
                        }

                        placements++;

                        if (IsSealed(board, tile, range))
                        {
                            sealedOff++;
                        }
                    }
                }
            }

            return sealedOff * 100 / placements;
        }

        /// <summary>Whether a bomb here covers a pocket with no walkable tile outside it.</summary>
        private static bool IsSealed(BoardState board, GridCoord origin, int range)
        {
            var covered = new System.Collections.Generic.HashSet<GridCoord> { origin };
            var cardinals = Directions.Cardinals;

            for (var d = 0; d < cardinals.Length; d++)
            {
                for (var distance = 1; distance <= range; distance++)
                {
                    var tile = origin.Step(cardinals[d], distance);
                    var type = board[tile];

                    if (type == TileType.Solid)
                    {
                        break;
                    }

                    covered.Add(tile);

                    if (type == TileType.Destructible)
                    {
                        break;
                    }
                }
            }

            foreach (var tile in covered)
            {
                if (!board.IsWalkable(tile))
                {
                    continue;
                }

                for (var d = 0; d < cardinals.Length; d++)
                {
                    var neighbour = tile.Neighbour(cardinals[d]);

                    if (board.IsWalkable(neighbour) && !covered.Contains(neighbour))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Every tile that is either feared or alight, as coordinates, for set comparison.
        /// </summary>
        private static GridCoord[] Tiles(GameSimulation simulation, bool threatened)
        {
            var found = new System.Collections.Generic.List<GridCoord>();
            var state = simulation.State;

            for (var y = 0; y < state.Board.Height; y++)
            {
                for (var x = 0; x < state.Board.Width; x++)
                {
                    var tile = new GridCoord(x, y);
                    var hit = threatened
                        ? state.Threats.IsThreatened(tile)
                        : state.BlastGrid.IsLethal(tile);

                    if (hit)
                    {
                        found.Add(tile);
                    }
                }
            }

            return found.ToArray();
        }
    }
}
