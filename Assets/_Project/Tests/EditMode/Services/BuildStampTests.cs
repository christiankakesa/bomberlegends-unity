using BomberLegends.Services.Diagnostics;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Services
{
    /// <summary>
    /// Covers reading the commit back out of a player's version string.
    /// </summary>
    public sealed class BuildStampTests
    {
        [Test]
        public void TheCommitIsWhatFollowsThePlus()
        {
            Assert.That(BuildStamp.CommitIn("1.0+ab12cd3"), Is.EqualTo("ab12cd3"));
        }

        [Test]
        public void ADirtyTreeKeepsItsStar()
        {
            // The one character that separates "this build is that commit" from "this build is
            // roughly that commit", which is the difference between a repro and a guess.
            Assert.That(BuildStamp.CommitIn("1.0+ab12cd3*"), Is.EqualTo("ab12cd3*"));
        }

        [Test]
        public void AVersionWithoutAStampSaysSo()
        {
            // The Editor, and any build made without the processor. Neither must pass as a commit.
            Assert.That(BuildStamp.CommitIn("1.0"), Is.EqualTo(BuildStamp.Unstamped));
            Assert.That(BuildStamp.CommitIn("1.0+"), Is.EqualTo(BuildStamp.Unstamped));
            Assert.That(BuildStamp.CommitIn(string.Empty), Is.EqualTo(BuildStamp.Unstamped));
        }

        [Test]
        public void TheBasisIsTheVersionWithTheStampTakenOff()
        {
            // Stamping onto a stamped version would read 1.0+abc+def and grow by one commit per
            // build. The processor strips first, and this is what it strips with.
            Assert.That(BuildStamp.Basis("1.0+ab12cd3"), Is.EqualTo("1.0"));
            Assert.That(BuildStamp.Basis("1.0"), Is.EqualTo("1.0"));
        }

        [Test]
        public void TheLabelNamesTheFlavour()
        {
            Assert.That(BuildStamp.Describe("1.0+ab12cd3", isEditor: false, isDebug: true),
                Is.EqualTo("ab12cd3 · DEV"));
            Assert.That(BuildStamp.Describe("1.0+ab12cd3", isEditor: false, isDebug: false),
                Is.EqualTo("ab12cd3 · REL"));
            Assert.That(BuildStamp.Describe("1.0", isEditor: true, isDebug: true),
                Is.EqualTo("unstamped · EDITOR"));
        }

        [Test]
        public void ThePercentileIsTheNearestRank()
        {
            // The 99th of six hundred frames is the sixth-worst frame, not a blend of two frames
            // neither of which happened.
            var sorted = new float[600];
            for (var i = 0; i < sorted.Length; i++)
            {
                sorted[i] = i;
            }

            Assert.That(DeviceLogOverlay.Percentile(sorted, 600, 0.5f), Is.EqualTo(299f));
            Assert.That(DeviceLogOverlay.Percentile(sorted, 600, 0.99f), Is.EqualTo(593f));
            Assert.That(DeviceLogOverlay.Percentile(sorted, 600, 1f), Is.EqualTo(599f));
            Assert.That(DeviceLogOverlay.Percentile(sorted, 0, 0.5f), Is.EqualTo(0f),
                "no frames yet is zero, not an index out of range");
        }
    }
}
