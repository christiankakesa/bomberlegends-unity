using BomberLegends.Core;
using BomberLegends.Gameplay.Match;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers what the match readout says about bombs.
    /// </summary>
    /// <remarks>
    /// The skills carried this defect first: a playtester hoarded both of them for an entire run
    /// because nothing on screen said they came back. The bomb had the same silence and a worse
    /// consequence, being the verb the whole game is built on — a player holding a button that does
    /// nothing learns that bombs are unreliable rather than that they are on a timer.
    /// </remarks>
    public sealed class MatchHudTests
    {
        private const int Fuse = 90;

        private GameObject? _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        [Test]
        public void ABombInHandNeedsNoWait()
        {
            Assert.That(MatchHudView.TicksUntilABombReturns(
                inHand: 1, soonestFuseTicks: 0, cooldownTicksRemaining: 0), Is.Zero);
        }

        [Test]
        public void WithNothingInHandTheWaitIsTheSoonestFuse()
        {
            // Two bombs down, the older one about to go: what the player is waiting for is the
            // first slot to free up, not the last.
            Assert.That(MatchHudView.TicksUntilABombReturns(
                inHand: 0, soonestFuseTicks: 12, cooldownTicksRemaining: 0), Is.EqualTo(12));
        }

        [Test]
        public void ACooldownCountsEvenWithABombInHand()
        {
            // The other bomb economy — a cooldown that starts when a bomb is placed — is one
            // Inspector value away and gets tried on a device. Under it the player can be holding a
            // bomb they are not yet allowed to place, and the readout has to say so.
            Assert.That(MatchHudView.TicksUntilABombReturns(
                inHand: 1, soonestFuseTicks: 0, cooldownTicksRemaining: 20), Is.EqualTo(20));
        }

        [Test]
        public void TheWaitIsWhicheverClearsLast()
        {
            // Not the sum. Both run down together, so the player waits for the slower of the two.
            Assert.That(MatchHudView.TicksUntilABombReturns(
                inHand: 0, soonestFuseTicks: Fuse, cooldownTicksRemaining: 20), Is.EqualTo(Fuse));

            Assert.That(MatchHudView.TicksUntilABombReturns(
                inHand: 0, soonestFuseTicks: 20, cooldownTicksRemaining: Fuse), Is.EqualTo(Fuse));
        }

        /// <summary>
        /// The readout end to end, because the rule being right is only half of it.
        /// </summary>
        /// <remarks>
        /// Reads the same simulation the match does rather than a hand-built state, so a field
        /// renamed or a counter read from the wrong place fails here instead of on a device.
        /// </remarks>
        [Test]
        public void PlacingTheOnlyBombTurnsTheCountIntoACountdown()
        {
            var simulation = OpenRoom();
            var (hud, output) = BuildHud();

            hud.Render(simulation);
            Assert.That(output.text, Does.Contain("BOMBS 1"),
                "an untouched player is carrying a bomb and the readout should say so");

            simulation.Tick(new PlayerIntent(0, 0, IntentButtons.Bomb));
            hud.Render(simulation);

            Assert.That(output.text, Does.Match(@"BOMBS [0-3]\.\ds"),
                $"with the only bomb on the board the readout must count it back in: \"{output.text}\"");

            // Past the fuse and its blast: the bomb is gone, the player has it again.
            for (var tick = 0; tick < Fuse + 30; tick++)
            {
                simulation.Tick(PlayerIntent.None);
            }

            hud.Render(simulation);

            Assert.That(output.text, Does.Contain("BOMBS 1"),
                $"the bomb detonated and never came back to the readout: \"{output.text}\"");
        }

        /// <summary>
        /// The empty slot is named rather than left out.
        /// </summary>
        /// <remarks>
        /// Testers asked why there were only two skills. That is not a complaint about the number
        /// — it is that the ceiling was unexplained, and an unexplained ceiling reads as the end of
        /// the game rather than as the part not built yet.
        /// </remarks>
        [Test]
        public void TheSlotWithNoSkillInItSaysSo()
        {
            var simulation = OpenRoom();
            var (hud, output) = BuildHud();

            hud.Render(simulation);

            Assert.That(output.text, Does.Contain("LOCKED"),
                $"the third slot is empty and the readout does not mention it: \"{output.text}\"");

            // Named after the skills it will join, not before them, or the promise would read as
            // the first thing the player has.
            Assert.That(output.text.IndexOf("LOCKED", System.StringComparison.Ordinal),
                Is.GreaterThan(output.text.IndexOf("DASH", System.StringComparison.Ordinal)));
        }

        private static GameSimulation OpenRoom() =>
            new GameSimulation(
                SimulationConfig.Default,
                LevelLayout.Parse(
                    "#######",
                    "#.....#",
                    "#.....#",
                    "#..P..#",
                    "#.....#",
                    "#.....#",
                    "#######"),
                seed: 1u);

        /// <summary>
        /// Builds a readout wired to a label, the way the scene does it.
        /// </summary>
        /// <remarks>
        /// Through <see cref="SerializedObject"/> rather than a test-only setter: the field is
        /// serialized and assigned in the scene, and a setter added for a test is a second way of
        /// wiring it that the shipped game never uses.
        /// </remarks>
        private (MatchHudView Hud, Text Output) BuildHud()
        {
            _root = new GameObject("Hud", typeof(Text), typeof(MatchHudView));

            var hud = _root.GetComponent<MatchHudView>();
            var output = _root.GetComponent<Text>();

            var serialized = new SerializedObject(hud);
            serialized.FindProperty("_output").objectReferenceValue = output;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return (hud, output);
        }
    }
}
