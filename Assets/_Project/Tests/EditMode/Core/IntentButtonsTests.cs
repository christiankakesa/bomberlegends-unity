using BomberLegends.Core;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Core
{
    /// <summary>Covers the boxing-free flag helpers used in per-tick input handling.</summary>
    public sealed class IntentButtonsTests
    {
        [Test]
        public void None_HasNoFlagsSet()
        {
            Assert.That(IntentButtons.None.Has(IntentButtons.Bomb), Is.False);
            Assert.That(IntentButtons.None.Has(IntentButtons.Special), Is.False);
            Assert.That(IntentButtons.None.Has(IntentButtons.Sprint), Is.False);
        }

        [Test]
        public void Flags_AreDistinctBits()
        {
            Assert.That((byte)IntentButtons.Bomb, Is.EqualTo(1));
            Assert.That((byte)IntentButtons.Special, Is.EqualTo(2));
            Assert.That((byte)IntentButtons.Sprint, Is.EqualTo(4));
        }

        [Test]
        public void Has_DetectsOnlyTheFlagsThatAreSet()
        {
            var buttons = IntentButtons.Bomb | IntentButtons.Sprint;

            Assert.That(buttons.Has(IntentButtons.Bomb), Is.True);
            Assert.That(buttons.Has(IntentButtons.Sprint), Is.True);
            Assert.That(buttons.Has(IntentButtons.Special), Is.False);
        }

        [Test]
        public void With_SetsAFlag_AndIsIdempotent()
        {
            var buttons = IntentButtons.None.With(IntentButtons.Special);

            Assert.That(buttons.Has(IntentButtons.Special), Is.True);
            Assert.That(buttons.With(IntentButtons.Special), Is.EqualTo(buttons));
        }

        [Test]
        public void Without_ClearsOnlyTheGivenFlag()
        {
            var buttons = (IntentButtons.Bomb | IntentButtons.Special).Without(IntentButtons.Bomb);

            Assert.That(buttons.Has(IntentButtons.Bomb), Is.False);
            Assert.That(buttons.Has(IntentButtons.Special), Is.True);
        }

        [Test]
        public void Without_OnAnUnsetFlag_ChangesNothing()
        {
            var buttons = IntentButtons.Special;

            Assert.That(buttons.Without(IntentButtons.Bomb), Is.EqualTo(IntentButtons.Special));
        }

        [Test]
        public void AllFlagsSet_ReportsEveryButton()
        {
            var buttons = IntentButtons.Bomb | IntentButtons.Special | IntentButtons.Sprint;

            foreach (var flag in new[] { IntentButtons.Bomb, IntentButtons.Special, IntentButtons.Sprint })
            {
                Assert.That(buttons.Has(flag), Is.True);
            }
        }
    }
}
