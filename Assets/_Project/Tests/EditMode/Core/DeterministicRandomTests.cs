using System;
using BomberLegends.Core;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Core
{
    /// <summary>
    /// Covers the guarantee the whole simulation depends on: identical seeds produce identical
    /// sequences, and every range is drawn without bias.
    /// </summary>
    public sealed class DeterministicRandomTests
    {
        private const int DrawCount = 10_000;

        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            var first = new DeterministicRandom(12345u);
            var second = new DeterministicRandom(12345u);

            for (var i = 0; i < DrawCount; i++)
            {
                Assert.That(second.NextUInt(), Is.EqualTo(first.NextUInt()), $"sequences diverged at draw {i}");
            }
        }

        [Test]
        public void SameSeed_ProducesIdenticalState_AfterManyDraws()
        {
            var first = new DeterministicRandom(0xC0FFEEu);
            var second = new DeterministicRandom(0xC0FFEEu);

            for (var i = 0; i < DrawCount; i++)
            {
                first.NextUInt();
                second.NextUInt();
            }

            Assert.That(second.State, Is.EqualTo(first.State));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void DifferentSeeds_Diverge()
        {
            var first = new DeterministicRandom(1u);
            var second = new DeterministicRandom(2u);

            var identical = 0;
            for (var i = 0; i < 1000; i++)
            {
                if (first.NextUInt() == second.NextUInt())
                {
                    identical++;
                }
            }

            Assert.That(identical, Is.LessThan(10), "streams from different seeds should not track each other");
        }

        [Test]
        public void ZeroSeed_IsReproducibleAndNotDegenerate()
        {
            var generator = new DeterministicRandom(0u);
            var reference = new DeterministicRandom(0u);

            var allZero = true;
            for (var i = 0; i < 100; i++)
            {
                var value = generator.NextUInt();
                Assert.That(reference.NextUInt(), Is.EqualTo(value));

                if (value != 0u)
                {
                    allZero = false;
                }
            }

            Assert.That(allZero, Is.False, "a zero seed must not collapse the generator to zeroes");
        }

        [Test]
        public void NextUInt_AdvancesState()
        {
            var generator = new DeterministicRandom(7u);
            var before = generator.State;

            generator.NextUInt();

            Assert.That(generator.State, Is.Not.EqualTo(before));
        }

        [Test]
        public void NextUInt_NeverReachesZeroState()
        {
            var generator = new DeterministicRandom(1u);

            for (var i = 0; i < DrawCount; i++)
            {
                generator.NextUInt();
                Assert.That(generator.State, Is.Not.Zero, $"state collapsed to zero at draw {i}");
            }
        }

        [Test]
        public void NextInt_StaysWithinBounds()
        {
            var generator = new DeterministicRandom(99u);

            for (var i = 0; i < DrawCount; i++)
            {
                var value = generator.NextInt(13);
                Assert.That(value, Is.InRange(0, 12));
            }
        }

        [Test]
        public void NextInt_WithBoundOfOne_AlwaysReturnsZero()
        {
            var generator = new DeterministicRandom(5u);

            for (var i = 0; i < 100; i++)
            {
                Assert.That(generator.NextInt(1), Is.EqualTo(0));
            }
        }

        [Test]
        public void NextInt_CoversItsWholeRange()
        {
            var generator = new DeterministicRandom(2024u);
            var seen = new bool[6];

            for (var i = 0; i < DrawCount; i++)
            {
                seen[generator.NextInt(6)] = true;
            }

            Assert.That(seen, Is.All.True, "every outcome in the range should occur");
        }

        [Test]
        public void NextInt_IsFreeOfSignificantBias()
        {
            var generator = new DeterministicRandom(31337u);
            var counts = new int[4];

            for (var i = 0; i < DrawCount; i++)
            {
                counts[generator.NextInt(4)]++;
            }

            foreach (var count in counts)
            {
                Assert.That(count, Is.InRange(2300, 2700), "distribution is skewed beyond sampling noise");
            }
        }

        [Test]
        public void NextInt_WithInvalidBound_Throws()
        {
            var generator = new DeterministicRandom(1u);

            Assert.Throws<ArgumentOutOfRangeException>(() => generator.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => generator.NextInt(-5));
        }

        [Test]
        public void NextInt_WithRange_StaysWithinBounds()
        {
            var generator = new DeterministicRandom(77u);

            for (var i = 0; i < DrawCount; i++)
            {
                Assert.That(generator.NextInt(-3, 4), Is.InRange(-3, 3));
            }
        }

        [Test]
        public void NextInt_WithInvertedRange_Throws()
        {
            var generator = new DeterministicRandom(1u);

            Assert.Throws<ArgumentOutOfRangeException>(() => generator.NextInt(5, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => generator.NextInt(5, 4));
        }

        [Test]
        public void NextBool_ProducesBothOutcomes()
        {
            var generator = new DeterministicRandom(4242u);
            var trueCount = 0;

            for (var i = 0; i < DrawCount; i++)
            {
                if (generator.NextBool())
                {
                    trueCount++;
                }
            }

            Assert.That(trueCount, Is.InRange(4700, 5300));
        }

        [TestCase(0)]
        [TestCase(-25)]
        public void Chance_AtOrBelowZero_NeverSucceeds(int percent)
        {
            var generator = new DeterministicRandom(11u);

            for (var i = 0; i < 500; i++)
            {
                Assert.That(generator.Chance(percent), Is.False);
            }
        }

        [TestCase(100)]
        [TestCase(150)]
        public void Chance_AtOrAboveOneHundred_AlwaysSucceeds(int percent)
        {
            var generator = new DeterministicRandom(11u);

            for (var i = 0; i < 500; i++)
            {
                Assert.That(generator.Chance(percent), Is.True);
            }
        }

        [Test]
        public void Chance_ApproximatesTheRequestedRate()
        {
            var generator = new DeterministicRandom(8080u);
            var successes = 0;

            for (var i = 0; i < DrawCount; i++)
            {
                if (generator.Chance(40))
                {
                    successes++;
                }
            }

            Assert.That(successes, Is.InRange(3700, 4300));
        }

        [Test]
        public void Equality_ComparesState()
        {
            var a = new DeterministicRandom(500u);
            var b = new DeterministicRandom(500u);
            var c = new DeterministicRandom(501u);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.Equals((object)b), Is.True);
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.Equals("not a generator"), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_ShowsState()
        {
            Assert.That(new DeterministicRandom(0xABCDEF01u).ToString(), Is.EqualTo("Random(0xABCDEF01)"));
        }

        [Test]
        public void PassedByValue_DoesNotAdvanceTheOriginal()
        {
            var original = new DeterministicRandom(64u);
            var copy = original;

            copy.NextUInt();

            Assert.That(original.State, Is.Not.EqualTo(copy.State),
                "a copy must advance independently; the simulation passes this type by ref for that reason");
        }
    }
}
