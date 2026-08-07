using System.Collections.Generic;
using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Items;
using BomberLegends.Simulation.Run;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers the run loop: clear an arena, choose an item, carry the build forward, die, restart.
    /// </summary>
    public sealed class GameRunTests
    {
        private const int Fuse = 30;

        private static SimulationConfig Config(int healing = 25, int itemSlots = 2) =>
            new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true,
                fuseTicks: Fuse,
                blastLingerTicks: 12,
                bombCooldownTicks: 0,
                startingBombCapacity: 1,
                startingBlastRange: 2,
                maxBombs: 16,
                playerRadius: 340,
                cornerSlipPerTick: 90,
                cornerSlipTolerance: 320,
                playerMaxHealth: 100,
                blastDamageToPlayer: 34,
                enemyContactDamage: 10,
                invulnerabilityTicks: 30,
                enemyMaxHealth: 100,
                blastDamageToEnemy: 100,
                enemySpeedPerTick: 1,
                enemyRadius: 320,
                maxEnemies: 32,
                itemSlots: itemSlots,
                arenaClearHealing: healing);

        /// <summary>One enemy, two tiles east of the player and inside a range-2 blast.</summary>
        private static LevelLayout OneEnemy => LevelLayout.Parse(
            "#########",
            "#P.E....#",
            "#########");

        /// <summary>One enemy far out of blast reach, so dying does not also clear the arena.</summary>
        private static LevelLayout FarEnemy => LevelLayout.Parse(
            "############",
            "#P........E#",
            "############");

        private static LevelLayout[] Arenas(int count)
        {
            var arenas = new LevelLayout[count];
            for (var i = 0; i < count; i++)
            {
                arenas[i] = OneEnemy;
            }

            return arenas;
        }

        private static PlayerIntent Bomb => new PlayerIntent(0, 0, IntentButtons.Bomb);

        private static PlayerIntent Idle => PlayerIntent.None;

        /// <summary>Bombs the adjacent enemy and runs the run forward until the phase settles.</summary>
        private static void ClearArena(GameRun run)
        {
            run.Current.Tick(Bomb);
            run.Observe();

            for (var i = 0; i < Fuse + 20 && run.Phase == RunPhase.Fighting; i++)
            {
                run.Current.Tick(Idle);
                run.Observe();
            }
        }

        // ---------- clearing ----------

        [Test]
        public void KillingEveryEnemy_ClearsTheArena()
        {
            var simulation = new GameSimulation(Config(), OneEnemy, seed: 1u);

            simulation.Tick(Bomb);
            for (var i = 0; i < Fuse + 5; i++)
            {
                simulation.Tick(Idle);
            }

            Assert.That(simulation.Phase, Is.EqualTo(MatchPhase.Victory));
        }

        [Test]
        public void AnArenaWithNoEnemies_IsNotInstantlyWon()
        {
            // A sandbox room must stay a sandbox, or every movement test would end on tick one.
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#####",
                    "#.P.#",
                    "#####"),
                seed: 1u);

            for (var i = 0; i < 120; i++)
            {
                simulation.Tick(Idle);
            }

            Assert.That(simulation.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        // ---------- the loop ----------

        [Test]
        public void ARunStartsFightingTheFirstArena()
        {
            var run = new GameRun(Config(), Arenas(3), seed: 1u);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Fighting));
            Assert.That(run.ArenaNumber, Is.EqualTo(1));
            Assert.That(run.Held.Length, Is.Zero);
        }

        [Test]
        public void ClearingAnArena_OffersAChoice()
        {
            var run = new GameRun(Config(), Arenas(3), seed: 1u);

            ClearArena(run);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Choosing));
            Assert.That(run.Offers.Length, Is.EqualTo(3));
            Assert.That(run.ArenaNumber, Is.EqualTo(1),
                "the number shown must be the arena just cleared, not the one coming");
        }

        [Test]
        public void ChoosingAnItem_MovesToTheNextArena()
        {
            var run = new GameRun(Config(), Arenas(3), seed: 1u);

            ClearArena(run);
            var picked = run.Offers[0];

            Assert.That(run.TryChoose(picked), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Fighting));
            Assert.That(run.ArenaNumber, Is.EqualTo(2));
            Assert.That(run.Held.ToArray(), Does.Contain(picked));
        }

        [Test]
        public void AnItemThatWasNotOffered_CannotBeTaken()
        {
            // Reached by taking an item first: what is held is never offered again, so it is
            // reliably absent from the second offer whatever the shuffle produced.
            var run = new GameRun(Config(), Arenas(4), seed: 1u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            var held = run.Held[0];

            ClearArena(run);
            Assume.That(run.Phase, Is.EqualTo(RunPhase.Choosing));
            Assert.That(run.Offers.ToArray(), Has.No.Member(held));

            Assert.That(run.TryChoose(held), Is.False, "an unoffered item must be refused");
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Choosing), "and the choice must still stand");
        }

        [Test]
        public void NothingCanBeChosenWhileAnArenaIsBeingFought()
        {
            var run = new GameRun(Config(), Arenas(3), seed: 1u);

            Assume.That(run.Phase, Is.EqualTo(RunPhase.Fighting));

            Assert.That(run.TryChoose(ItemId.KineticCore), Is.False);
            Assert.That(run.Held.Length, Is.Zero);
        }

        [Test]
        public void TheBuildCarriesIntoTheNextArena()
        {
            var run = new GameRun(Config(), Arenas(3), seed: 1u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);

            var held = run.Held[0];

            Assert.That(run.Current.State.Player.Items.Contains(held), Is.True,
                "the new arena's simulation must already know about the build");
        }

        [Test]
        public void AnItemsEffectCarriesToo_NotJustItsName()
        {
            // Re-granting must reproduce the build, not merely list it.
            var run = new GameRun(Config(), Arenas(3), seed: 1u);
            var before = run.Current.State.Player.Skills[0].Magnitude;

            ClearArena(run);
            run.TryChoose(ItemId.KineticCore);

            Assert.That(run.Current.State.Player.Skills[0].Magnitude, Is.EqualTo(before * 3 / 2));
        }

        [Test]
        public void DamageCarriesBetweenArenas()
        {
            var run = new GameRun(Config(healing: 0), Arenas(3), seed: 1u);

            // Stand in the blast that clears the arena.
            ClearArena(run);
            var hurt = run.Current.State.Player.Health.Current;

            Assume.That(hurt, Is.LessThan(100), "the clearing blast must have caught the player");

            run.TryChoose(run.Offers[0]);

            Assert.That(run.Current.State.Player.Health.Current, Is.EqualTo(hurt),
                "a run is a resource to manage, not a series of separate fights");
        }

        [Test]
        public void ClearingAnArena_RestoresSomeHealthButNotAll()
        {
            var run = new GameRun(Config(healing: 25), Arenas(3), seed: 1u);

            ClearArena(run);
            var hurt = run.Current.State.Player.Health.Current;

            Assume.That(hurt, Is.LessThan(100));

            run.TryChoose(run.Offers[0]);
            var healed = run.Current.State.Player.Health.Current;

            Assert.That(healed, Is.GreaterThan(hurt), "clearing must be worth something");
            Assert.That(healed, Is.LessThanOrEqualTo(100), "and must never exceed full health");
        }

        [Test]
        public void WithSlotsFull_TheOfferBecomesASwap()
        {
            // Late in a run the question stops being "what do I want?" and becomes "what am I
            // willing to give up?" — which is why a full inventory keeps getting offers.
            var run = new GameRun(Config(itemSlots: 1), Arenas(4), seed: 1u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            var first = run.Held[0];

            ClearArena(run);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Choosing));

            var taking = run.Offers[0];
            Assert.That(run.TryChoose(taking), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Discarding),
                "with no free slot, taking something must first cost something");
            Assert.That(run.Pending, Is.EqualTo(taking));

            Assert.That(run.TryDiscard(first), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Fighting));
            Assert.That(run.Held.ToArray(), Is.EqualTo(new[] { taking }));
        }

        [Test]
        public void AnOfferCanBeDeclined()
        {
            // Without this a late run would force a player to break a build they are happy with,
            // turning a decision into a penalty for having chosen well.
            var run = new GameRun(Config(itemSlots: 1), Arenas(4), seed: 1u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            var kept = run.Held[0];

            ClearArena(run);
            Assert.That(run.Skip(), Is.True);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Fighting));
            Assert.That(run.ArenaNumber, Is.EqualTo(3));
            Assert.That(run.Held.ToArray(), Is.EqualTo(new[] { kept }), "skipping keeps the build");
        }

        [Test]
        public void ASwapCanBeAbandonedPartWayThrough()
        {
            var run = new GameRun(Config(itemSlots: 1), Arenas(4), seed: 1u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            var kept = run.Held[0];

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            Assume.That(run.Phase, Is.EqualTo(RunPhase.Discarding));

            Assert.That(run.Skip(), Is.True);
            Assert.That(run.Held.ToArray(), Is.EqualTo(new[] { kept }));
            Assert.That(run.Pending, Is.EqualTo(ItemId.None));
        }

        [Test]
        public void SomethingNotHeldCannotBeGivenUp()
        {
            var run = new GameRun(Config(itemSlots: 1), Arenas(4), seed: 1u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            Assume.That(run.Phase, Is.EqualTo(RunPhase.Discarding));

            Assert.That(run.TryDiscard(ItemId.None), Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Discarding));
        }

        [Test]
        public void ASwappedAwayItemStopsAffectingTheBuild()
        {
            // The M5 note said items could never be removed. That limit was inside one simulation;
            // a run rebuilds the loadout per arena, so a swap genuinely undoes the old item.
            var run = new GameRun(
                Config(itemSlots: 1), Arenas(4), seed: 1u, startingItems: new[] { ItemId.Momentum });

            Assume.That(run.Current.State.Player.Skills[0].Power, Is.EqualTo(40));

            ClearArena(run);
            Assume.That(run.Phase, Is.EqualTo(RunPhase.Choosing));

            run.TryChoose(run.Offers[0]);
            Assume.That(run.Phase, Is.EqualTo(RunPhase.Discarding));
            run.TryDiscard(ItemId.Momentum);

            Assert.That(run.Current.State.Player.Skills[0].Power, Is.Zero,
                "the discarded item's effect must be gone, not merely unlisted");
        }

        [Test]
        public void OffersNeverIncludeSomethingAlreadyHeld()
        {
            var run = new GameRun(Config(), Arenas(4), seed: 3u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            var held = run.Held[0];

            ClearArena(run);

            foreach (var offer in run.Offers.ToArray())
            {
                Assert.That(offer, Is.Not.EqualTo(held));
            }
        }

        [Test]
        public void ArenasAdvanceThroughTheAuthoredOrder()
        {
            var arenas = new[]
            {
                OneEnemy,
                LevelLayout.Parse(
                    "###########",
                    "#P.E......#",
                    "###########")
            };

            var run = new GameRun(Config(), arenas, seed: 1u);
            Assert.That(run.Current.State.Board.Width, Is.EqualTo(9));

            ClearArena(run);
            run.TryChoose(run.Offers[0]);

            Assert.That(run.Current.State.Board.Width, Is.EqualTo(11));
        }

        // ---------- death and restart ----------

        [Test]
        public void DyingEndsTheRun()
        {
            // The enemy is far out of blast reach, so the bombs that kill the player cannot also
            // clear the arena — otherwise this would measure victory, not death.
            var run = new GameRun(
                Config(), new[] { FarEnemy, FarEnemy, FarEnemy }, seed: 1u);

            // Three of your own blasts is fatal.
            for (var attempt = 0; attempt < 6 && run.Phase == RunPhase.Fighting; attempt++)
            {
                run.Current.Tick(Bomb);
                for (var i = 0; i < Fuse + 45 && run.Phase == RunPhase.Fighting; i++)
                {
                    run.Current.Tick(Idle);
                    run.Observe();
                }
            }

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Ended));
        }

        [Test]
        public void RestartingGivesACleanRun()
        {
            var run = new GameRun(Config(), Arenas(3), seed: 1u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            Assume.That(run.ArenaNumber, Is.EqualTo(2));

            run.Restart();

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Fighting));
            Assert.That(run.ArenaNumber, Is.EqualTo(1));
            Assert.That(run.Held.Length, Is.Zero, "a clean restart keeps nothing");
            Assert.That(run.Current.State.Player.Health.Current, Is.EqualTo(100));
            Assert.That(run.Current.State.Enemies.AliveCount, Is.EqualTo(1));
        }

        [Test]
        public void RestartingDoesNotTouchTheDisk()
        {
            // The whole reason a restart is cheap: it builds objects, it does not load anything.
            // Players who just died want to be playing again, not watching a loading bar.
            var run = new GameRun(Config(), Arenas(3), seed: 1u);

            var before = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 0; i < 200; i++)
            {
                run.Restart();
            }

            before.Stop();

            Assert.That(before.ElapsedMilliseconds, Is.LessThan(500),
                "two hundred restarts must be near-instant, or a single one is not free");
        }

        // ---------- starting build ----------

        [Test]
        public void AStartingBuildSurvivesARestart()
        {
            var run = new GameRun(
                Config(), Arenas(3), seed: 1u, startingItems: new[] { ItemId.Momentum });

            Assert.That(run.Held.ToArray(), Does.Contain(ItemId.Momentum));

            run.Restart();

            Assert.That(run.Held.ToArray(), Does.Contain(ItemId.Momentum));
            Assert.That(run.Current.State.Player.Skills[0].Power, Is.EqualTo(40));
        }

        [Test]
        public void AStartingBuildOccupiesRealSlots()
        {
            var run = new GameRun(
                Config(itemSlots: 2),
                Arenas(3),
                seed: 1u,
                startingItems: new[] { ItemId.Momentum, ItemId.Overcharge });

            ClearArena(run);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Choosing),
                "a full inventory still gets an offer; it is simply a swap");
            Assert.That(run.Held.Length, Is.EqualTo(2));
        }

        // ---------- guarantees ----------

        [Test]
        public void ARunIsReproducibleFromItsSeed()
        {
            GameRun Create() => new GameRun(Config(), Arenas(4), seed: 777u);

            var first = Create();
            var second = Create();

            var choicesMade = 0;

            for (var arena = 0; arena < 4; arena++)
            {
                ClearArena(first);
                ClearArena(second);

                Assert.That(second.Phase, Is.EqualTo(first.Phase),
                    $"the runs disagreed about what happens after arena {arena + 1}");
                Assert.That(first.Phase, Is.EqualTo(RunPhase.Choosing),
                    "with nine items and two slots, every clear must still present a decision");

                Assert.That(second.Offers.ToArray(), Is.EqualTo(first.Offers.ToArray()),
                    $"the offer on arena {arena + 1} must be part of the reproducible run");

                first.TryChoose(first.Offers[0]);
                second.TryChoose(second.Offers[0]);
                choicesMade++;

                if (first.Phase == RunPhase.Discarding)
                {
                    first.TryDiscard(first.Held[0]);
                    second.TryDiscard(second.Held[0]);
                }

                Assert.That(
                    second.Current.ComputeStateHash(), Is.EqualTo(first.Current.ComputeStateHash()));
            }

            Assert.That(choicesMade, Is.EqualTo(4),
                "a decision after every arena is the point of widening the pool");
        }

        [Test]
        public void ARunNeedsAtLeastOneArena()
        {
            Assert.Throws<System.ArgumentException>(
                () => new GameRun(Config(), System.Array.Empty<LevelLayout>(), seed: 1u));
        }
    }
}
