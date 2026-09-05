using BomberLegends.Gameplay.Run;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers how an attempt at a run begins: which seed, which arena.
    /// </summary>
    public sealed class RunStartTests
    {
        [Test]
        public void AFixedSeedIsHandedBackUnchangedEveryTime()
        {
            // Replayability: tuning a specific board wants the same seed on every attempt.
            var start = new RunStart(fixedSeed: 42u, startingArena: 1);

            Assert.That(start.IsFresh, Is.False);
            Assert.That(start.NextSeed(), Is.EqualTo(42u));
            Assert.That(start.NextSeed(), Is.EqualTo(42u));
        }

        [Test]
        public void ZeroAsksForAFreshSeedAndNeverGetsZeroBack()
        {
            // Zero is the request, so it must never be the answer — the next attempt would read it
            // as another request and the seed shown on screen would be a lie.
            var start = new RunStart(fixedSeed: 0u, startingArena: 1);

            Assert.That(start.IsFresh, Is.True);

            for (var i = 0; i < 200; i++)
            {
                Assert.That(start.NextSeed(), Is.Not.EqualTo(0u));
            }
        }

        [Test]
        public void FreshSeedsDiffer()
        {
            // Two attempts in a row that replayed each other would defeat the point of asking.
            var start = new RunStart(fixedSeed: 0u, startingArena: 1);

            var first = start.NextSeed();
            var second = start.NextSeed();
            var third = start.NextSeed();

            Assert.That(first == second && second == third, Is.False,
                "three fresh seeds came out identical");
        }

        [Test]
        public void TheStartingArenaCountsFromOneOutsideAndZeroInside()
        {
            Assert.That(new RunStart(1u, startingArena: 1).StartingArenaIndex, Is.EqualTo(0));
            Assert.That(new RunStart(1u, startingArena: 1).StartsDeep, Is.False);

            Assert.That(new RunStart(1u, startingArena: 9).StartingArenaIndex, Is.EqualTo(8));
            Assert.That(new RunStart(1u, startingArena: 9).StartsDeep, Is.True);
        }

        [Test]
        public void AnArenaBelowOneMeansTheFirst()
        {
            // The Inspector clamps at one; anything constructed in code that says otherwise is a
            // mistake to absorb rather than a run to refuse.
            Assert.That(new RunStart(1u, startingArena: 0).StartingArenaIndex, Is.EqualTo(0));
            Assert.That(new RunStart(1u, startingArena: -3).StartingArenaIndex, Is.EqualTo(0));
        }
    }
}
