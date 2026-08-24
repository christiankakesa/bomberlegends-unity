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

        /// <summary>
        /// A run standing at its second offer, with <paramref name="wanted"/> among the items on it.
        /// </summary>
        /// <remarks>
        /// An offer is a shuffle, so a test that needs a named item has to find a seed that puts it
        /// there. Still deterministic: the search runs the same generator and finds the same seed
        /// every time.
        /// </remarks>
        private static GameRun RunOfferingOnTheSecondPick(ItemId wanted)
        {
            for (var seed = 1u; seed <= 200u; seed++)
            {
                var run = new GameRun(Config(), Arenas(4), seed);

                ClearArena(run);
                run.TryChoose(run.Offers[0]);
                ClearArena(run);

                if (run.Phase == RunPhase.Choosing &&
                    System.Array.IndexOf(run.Offers.ToArray(), wanted) >= 0)
                {
                    return run;
                }
            }

            throw new AssertionException(
                $"no seed put {ItemCatalog.Name(wanted)} on the second offer");
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
            // Re-granting must reproduce the build, not merely list it. Kinetic Core is the item
            // whose effect reads cleanly off a single number, and it is held out of the first offer
            // now, so the run takes something else first and this waits for the second.
            var run = RunOfferingOnTheSecondPick(ItemId.KineticCore);
            var before = run.Current.State.Player.Skills[0].Magnitude;

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
        public void TheFirstOfferHoldsBackWhatOnlyMultipliesABuild()
        {
            // Round 3 has Overclock taken by 8 of 12 testers and never on the first pick, where
            // they called it useless (14-INSIGHTS §5). Swept across seeds because the offer is a
            // shuffle: one seed passing says nothing about a rule.
            for (var seed = 1u; seed <= 50u; seed++)
            {
                var run = new GameRun(Config(), Arenas(3), seed);

                ClearArena(run);
                Assume.That(run.Phase, Is.EqualTo(RunPhase.Choosing));

                foreach (var offer in run.Offers.ToArray())
                {
                    Assert.That(ItemCatalog.ScalesWithTheBuild(offer), Is.False,
                        $"seed {seed} offered {ItemCatalog.Name(offer)} to a player holding nothing");
                }
            }
        }

        [Test]
        public void OnceThereIsABuild_TheHeldBackItemsAreOfferedAgain()
        {
            // The gate is about the moment and not about the item. If a multiplier never came back
            // this would be a quiet deletion from the pool rather than a fix to the first pick.
            var offeredLater = false;

            for (var seed = 1u; seed <= 50u && !offeredLater; seed++)
            {
                var run = new GameRun(Config(itemSlots: 4), Arenas(4), seed);

                ClearArena(run);
                run.TryChoose(run.Offers[0]);
                ClearArena(run);

                foreach (var offer in run.Offers.ToArray())
                {
                    offeredLater |= ItemCatalog.ScalesWithTheBuild(offer);
                }
            }

            Assert.That(offeredLater, Is.True,
                "a scaling item must reach the second offer once there is a build to scale");
        }

        [Test]
        public void AStartingBuildIsStillABuild()
        {
            // The starting-items aid grants real items, so that first offer is not made over
            // nothing and there is no reason to withhold anything from it.
            var offered = false;

            for (var seed = 1u; seed <= 50u && !offered; seed++)
            {
                var run = new GameRun(
                    Config(itemSlots: 4), Arenas(3), seed, new[] { ItemId.Momentum });

                ClearArena(run);

                foreach (var offer in run.Offers.ToArray())
                {
                    offered |= ItemCatalog.ScalesWithTheBuild(offer);
                }
            }

            Assert.That(offered, Is.True);
        }

        [Test]
        public void EnoughOfThePoolSurvivesTheFirstOfferGate()
        {
            // The gate must never be the reason a first offer is short. Asserted against the pool
            // rather than against a run, so adding multipliers faster than concrete items fails
            // here instead of quietly shrinking the first choice.
            var concrete = 0;

            foreach (var id in ItemCatalog.All)
            {
                if (!ItemCatalog.ScalesWithTheBuild(id))
                {
                    concrete++;
                }
            }

            Assert.That(concrete, Is.GreaterThanOrEqualTo(GameRun.OfferCount));
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

        // ---------- resuming ----------

        [Test]
        public void AFreshRunHasNothingWorthResuming()
        {
            // Restoring a run that has made no progress is indistinguishable from starting one, and
            // would quietly pin every session to a single seed.
            var run = new GameRun(Config(), Arenas(4), seed: 1u);

            Assert.That(run.CreateSnapshot().HasProgress, Is.False);
        }

        [Test]
        public void ASnapshotCapturesTheRunAndPutsItBack()
        {
            var run = new GameRun(Config(), Arenas(4), seed: 77u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            var taken = run.Held[0];

            var snapshot = run.CreateSnapshot();
            Assume.That(snapshot.HasProgress, Is.True);

            var resumed = new GameRun(Config(), Arenas(4), seed: 1u);
            Assert.That(resumed.TryResume(snapshot), Is.True);

            Assert.That(resumed.ArenaNumber, Is.EqualTo(run.ArenaNumber));
            Assert.That(resumed.Held.ToArray(), Is.EqualTo(new[] { taken }));
            Assert.That(resumed.Phase, Is.EqualTo(RunPhase.Fighting));
        }

        [Test]
        public void AResumedRunRebuildsTheSameArena()
        {
            // The board is reconstructed from the seed rather than stored, so this is the assertion
            // that the reconstruction is actually faithful.
            var run = new GameRun(Config(), Arenas(4), seed: 512u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);

            var expected = run.Current.State.Board.ComputeHash();

            var resumed = new GameRun(Config(), Arenas(4), seed: 3u);
            resumed.TryResume(run.CreateSnapshot());

            Assert.That(resumed.Current.State.Board.ComputeHash(), Is.EqualTo(expected));
        }

        [Test]
        public void AResumedRunRegeneratesTheIdenticalProceduralArena()
        {
            // The authored-arena version of this proves nothing: that source returns a fixed layout
            // and never touches the generator. This is the path a real run uses.
            //
            // Driven from a snapshot rather than by playing, because a generated arena starts its
            // enemies six tiles away by design and cannot be cleared from spawn.
            var snapshot = new RunSnapshot(
                seed: 4242u,
                arenaIndex: 3,
                carriedHealth: 74,
                held: new[] { ItemId.Momentum, ItemId.KineticCore },
                offerState: 0x51ED270Bu);

            GameRun Resume(uint constructionSeed)
            {
                // A different construction seed on purpose, so anything the snapshot fails to carry
                // shows up as a different board rather than accidentally matching.
                var run = new GameRun(
                    Config(), new GeneratedArenaSource(ArenaSettings.Default), constructionSeed);

                Assert.That(run.TryResume(snapshot), Is.True);
                return run;
            }

            var first = Resume(1u);
            var second = Resume(9999u);

            Assert.That(second.Current.State.Board.ComputeHash(),
                Is.EqualTo(first.Current.State.Board.ComputeHash()),
                "the same snapshot must regenerate the very same arena, not merely a similar one");

            Assert.That(second.Current.State.Board.Width,
                Is.EqualTo(first.Current.State.Board.Width));
            Assert.That(second.Current.State.Enemies.AliveCount,
                Is.EqualTo(first.Current.State.Enemies.AliveCount));
            Assert.That(first.ArenaNumber, Is.EqualTo(4));
            Assert.That(first.Current.State.Player.Health.Current, Is.EqualTo(74));
        }

        [Test]
        public void AResumedArenaComesBackWholeRatherThanAsItWasLeft()
        {
            // Resuming restarts the arena, so destructible blocks blown up before the interruption
            // are back. Worth pinning down: it is the one way a correctly restored run looks wrong
            // to a player, who remembers the hole they made in it.
            var snapshot = new RunSnapshot(
                seed: 77u,
                arenaIndex: 2,
                carriedHealth: 90,
                held: new[] { ItemId.Overcharge },
                offerState: 0x2545F491u);

            var run = new GameRun(
                Config(), new GeneratedArenaSource(ArenaSettings.Default), seed: 5u);

            run.TryResume(snapshot);
            var pristine = run.Current.State.Board.ComputeHash();

            // Walk clear of the spawn pocket before bombing. The pocket is guaranteed empty for a
            // radius of two and the blast reaches exactly two, so a bomb placed at spawn destroys
            // nothing at all — the safety guarantee working exactly as intended.
            for (var i = 0; i < 90; i++)
            {
                run.Current.Tick(PlayerIntent.FromDirection(Direction.East));
            }

            run.Current.Tick(Bomb);
            for (var i = 0; i < Fuse + 40; i++)
            {
                run.Current.Tick(Idle);
            }

            Assume.That(run.Current.State.Board.ComputeHash(), Is.Not.EqualTo(pristine),
                "the bomb must actually have destroyed something");

            var resumed = new GameRun(
                Config(), new GeneratedArenaSource(ArenaSettings.Default), seed: 5u);
            resumed.TryResume(snapshot);

            Assert.That(resumed.Current.State.Board.ComputeHash(), Is.EqualTo(pristine),
                "the arena returns as it was entered, not as it was left");
        }

        [Test]
        public void AResumedRunKeepsTheEffectsOfItsBuild()
        {
            // Listing the items back is not enough; they have to be applied to the loadout again.
            var run = new GameRun(
                Config(), Arenas(4), seed: 5u, startingItems: new[] { ItemId.Momentum });

            ClearArena(run);
            run.TryChoose(run.Offers[0]);

            var resumed = new GameRun(Config(), Arenas(4), seed: 9u);
            resumed.TryResume(run.CreateSnapshot());

            Assert.That(resumed.Current.State.Player.Skills[0].Power, Is.EqualTo(40));
        }

        [Test]
        public void AResumedRunKeepsTheDamageAlreadyTaken()
        {
            var run = new GameRun(Config(healing: 0), Arenas(4), seed: 11u);

            ClearArena(run);
            var hurt = run.Current.State.Player.Health.Current;
            Assume.That(hurt, Is.LessThan(100));

            run.TryChoose(run.Offers[0]);

            var resumed = new GameRun(Config(healing: 0), Arenas(4), seed: 2u);
            resumed.TryResume(run.CreateSnapshot());

            Assert.That(resumed.Current.State.Player.Health.Current,
                Is.EqualTo(run.Current.State.Player.Health.Current));
        }

        [Test]
        public void AResumedRunDoesNotReofferWhatItAlreadyShowed()
        {
            // The offer generator is wound forward to where it was, so coming back does not hand the
            // player the same three items they were already choosing between.
            var run = new GameRun(Config(), Arenas(6), seed: 31u);

            ClearArena(run);
            var firstOffer = run.Offers.ToArray();
            run.TryChoose(run.Offers[0]);

            ClearArena(run);
            var secondOffer = run.Offers.ToArray();
            run.TryChoose(run.Offers[0]);

            var resumed = new GameRun(Config(), Arenas(6), seed: 4u);
            resumed.TryResume(run.CreateSnapshot());

            // Both runs are now on the same arena, so clearing each should present the same next
            // offer if the resumed one really did pick the sequence back up.
            ClearArena(run);
            ClearArena(resumed);

            Assume.That(run.Offers.Length, Is.GreaterThan(0), "there must be an offer to compare");

            Assert.That(resumed.Offers.ToArray(), Is.EqualTo(run.Offers.ToArray()),
                "a resumed run must continue the same sequence of offers");
            Assert.That(resumed.Offers.ToArray(), Is.Not.EqualTo(firstOffer));
            Assert.That(resumed.Offers.ToArray(), Is.Not.EqualTo(secondOffer),
                "and must not replay an offer the player already answered");
        }

        [Test]
        public void ADeadRunIsNotResumable()
        {
            // Health of zero is a finished run. Restoring it would drop the player into an arena
            // they are already dead in.
            var snapshot = new RunSnapshot(1u, 3, 0, new[] { ItemId.Momentum });

            Assert.That(snapshot.HasProgress, Is.False);
            Assert.That(new GameRun(Config(), Arenas(4), seed: 1u).TryResume(snapshot), Is.False);
        }

        [Test]
        public void RestartingAbandonsTheResumedRun()
        {
            var run = new GameRun(Config(), Arenas(4), seed: 6u);

            ClearArena(run);
            run.TryChoose(run.Offers[0]);
            run.Restart();

            Assert.That(run.CreateSnapshot().HasProgress, Is.False);
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
