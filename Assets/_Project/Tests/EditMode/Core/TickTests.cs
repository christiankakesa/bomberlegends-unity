using System;
using BomberLegends.Core;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Core
{
    /// <summary>Covers tick arithmetic, comparison and conversion to and from seconds.</summary>
    public sealed class TickTests
    {
        private const int TickRate = 30;

        [Test]
        public void Zero_IsTickZero()
        {
            Assert.That(Tick.Zero.Value, Is.EqualTo(0));
        }

        [TestCase(3f, 90)]
        [TestCase(0.5f, 15)]
        [TestCase(0f, 0)]
        [TestCase(150f, 4500)]
        public void FromSeconds_ConvertsAtTheGivenRate(float seconds, int expectedTicks)
        {
            Assert.That(Tick.FromSeconds(seconds, TickRate).Value, Is.EqualTo(expectedTicks));
        }

        [Test]
        public void FromSeconds_RoundsToNearestTick()
        {
            // 0.51s at 30Hz is 15.3 ticks, 0.49s is 14.7.
            Assert.That(Tick.FromSeconds(0.51f, TickRate).Value, Is.EqualTo(15));
            Assert.That(Tick.FromSeconds(0.49f, TickRate).Value, Is.EqualTo(15));
            Assert.That(Tick.FromSeconds(0.4f, TickRate).Value, Is.EqualTo(12));
        }

        [Test]
        public void FromSeconds_WithNegativeDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Tick.FromSeconds(-1f, TickRate));
        }

        [Test]
        public void FromSeconds_WithInvalidRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Tick.FromSeconds(1f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Tick.FromSeconds(1f, -30));
        }

        [Test]
        public void ToSeconds_IsTheInverseOfFromSeconds()
        {
            Assert.That(new Tick(90).ToSeconds(TickRate), Is.EqualTo(3f).Within(0.0001f));
            Assert.That(new Tick(0).ToSeconds(TickRate), Is.EqualTo(0f));
        }

        [Test]
        public void ToSeconds_WithInvalidRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Tick(30).ToSeconds(0));
        }

        [Test]
        public void Arithmetic_AdvancesAndRewinds()
        {
            var tick = new Tick(100);

            Assert.That((tick + 5).Value, Is.EqualTo(105));
            Assert.That((tick - 20).Value, Is.EqualTo(80));
        }

        [Test]
        public void Subtraction_BetweenTicks_YieldsElapsedTicks()
        {
            Assert.That(new Tick(120) - new Tick(90), Is.EqualTo(30));
            Assert.That(new Tick(90) - new Tick(120), Is.EqualTo(-30));
        }

        [Test]
        public void Comparison_OrdersByValue()
        {
            var earlier = new Tick(10);
            var later = new Tick(20);

            Assert.That(earlier < later, Is.True);
            Assert.That(later > earlier, Is.True);
            Assert.That(earlier <= new Tick(10), Is.True);
            Assert.That(later >= new Tick(20), Is.True);
            Assert.That(earlier.CompareTo(later), Is.LessThan(0));
            Assert.That(later.CompareTo(earlier), Is.GreaterThan(0));
            Assert.That(earlier.CompareTo(new Tick(10)), Is.EqualTo(0));
        }

        [Test]
        public void Equality_ComparesValue()
        {
            var a = new Tick(42);
            var b = new Tick(42);
            var c = new Tick(43);

            Assert.That(a == b, Is.True);
            Assert.That(a != c, Is.True);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.Equals((object)b), Is.True);
            Assert.That(a.Equals("not a tick"), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_IsReadable()
        {
            Assert.That(new Tick(42).ToString(), Is.EqualTo("t42"));
        }
    }
}
