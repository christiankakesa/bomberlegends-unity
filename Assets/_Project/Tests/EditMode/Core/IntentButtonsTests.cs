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
            Assert.That(IntentButtons.None.Has(IntentButtons.Skill1), Is.False);
            Assert.That(IntentButtons.None.Has(IntentButtons.Skill2), Is.False);
        }

        [Test]
        public void Flags_AreDistinctBits()
        {
            // These values are the replay and future wire format. Renaming a button is free;
            // renumbering one silently invalidates every run ever recorded.
            Assert.That((byte)IntentButtons.Bomb, Is.EqualTo(1));
            Assert.That((byte)IntentButtons.Skill1, Is.EqualTo(2));
            Assert.That((byte)IntentButtons.Skill2, Is.EqualTo(4));
            Assert.That((byte)IntentButtons.Skill3, Is.EqualTo(8));
        }

        [Test]
        public void Has_DetectsOnlyTheFlagsThatAreSet()
        {
            var buttons = IntentButtons.Bomb | IntentButtons.Skill2;

            Assert.That(buttons.Has(IntentButtons.Bomb), Is.True);
            Assert.That(buttons.Has(IntentButtons.Skill2), Is.True);
            Assert.That(buttons.Has(IntentButtons.Skill1), Is.False);
        }

        [Test]
        public void With_SetsAFlag_AndIsIdempotent()
        {
            var buttons = IntentButtons.None.With(IntentButtons.Skill1);

            Assert.That(buttons.Has(IntentButtons.Skill1), Is.True);
            Assert.That(buttons.With(IntentButtons.Skill1), Is.EqualTo(buttons));
        }

        [Test]
        public void Without_ClearsOnlyTheGivenFlag()
        {
            var buttons = (IntentButtons.Bomb | IntentButtons.Skill1).Without(IntentButtons.Bomb);

            Assert.That(buttons.Has(IntentButtons.Bomb), Is.False);
            Assert.That(buttons.Has(IntentButtons.Skill1), Is.True);
        }

        [Test]
        public void Without_OnAnUnsetFlag_ChangesNothing()
        {
            var buttons = IntentButtons.Skill1;

            Assert.That(buttons.Without(IntentButtons.Bomb), Is.EqualTo(IntentButtons.Skill1));
        }

        [Test]
        public void AllFlagsSet_ReportsEveryButton()
        {
            var buttons = IntentButtons.Bomb | IntentButtons.Skill1 | IntentButtons.Skill2;

            foreach (var flag in new[] { IntentButtons.Bomb, IntentButtons.Skill1, IntentButtons.Skill2 })
            {
                Assert.That(buttons.Has(flag), Is.True);
            }
        }
    }
}
