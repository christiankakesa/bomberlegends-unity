using System.Collections;
using BomberLegends.Bootstrap;
using BomberLegends.Core;
using BomberLegends.Gameplay.Match;
using BomberLegends.Services;
using BomberLegends.Services.Scenes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BomberLegends.Tests.PlayMode
{
    /// <summary>
    /// Verifies that a match left completely alone stays completely still.
    /// </summary>
    /// <remarks>
    /// Written to chase a defect seen only on device: entering a match and touching nothing left the
    /// player several tiles from spawn with a bomb placed. Reproducing it here rather than through
    /// screenshots turns a guess into something that can be stepped through.
    /// </remarks>
    public sealed class MatchIdleTests
    {
        private const int TimeoutFrames = 900;

        [UnityTest]
        public IEnumerator AMatchLeftAlone_KeepsThePlayerAtSpawnAndPlacesNoBombs()
        {
            yield return SceneManager.LoadSceneAsync(
                SceneService.NameOf(SceneId.Bootstrap), LoadSceneMode.Single);

            var context = yield_WaitForHub();
            while (context.MoveNext())
            {
                yield return context.Current;
            }

            var graph = Object.FindFirstObjectByType<GameBootstrap>()?.Context;
            Assert.That(graph, Is.Not.Null, "start-up never completed");

            _ = graph!.Scenes.TransitionToAsync(SceneId.Match);

            MatchRunner? runner = null;
            var frames = 0;
            while (frames < TimeoutFrames)
            {
                runner = Object.FindFirstObjectByType<MatchRunner>();
                if (runner != null && runner.Simulation != null)
                {
                    break;
                }

                frames++;
                yield return null;
            }

            Assert.That(runner, Is.Not.Null, "the match never started");
            Assert.That(runner!.Simulation, Is.Not.Null);

            var simulation = runner.Simulation!;
            var spawn = simulation.State.Player.Tile;
            var startTick = simulation.CurrentTick;

            // Three seconds of doing absolutely nothing.
            for (var i = 0; i < 180; i++)
            {
                yield return null;
            }

            Assert.That(simulation.CurrentTick, Is.GreaterThan(startTick),
                "the simulation should still be advancing");
            Assert.That(simulation.State.Player.Tile, Is.EqualTo(spawn),
                $"the player drifted from {spawn} to {simulation.State.Player.Tile} with no input");
            Assert.That(simulation.State.Player.ActiveBombs, Is.Zero,
                "a bomb was placed with no button pressed");
        }

        [UnityTest]
        public IEnumerator Pausing_HoldsTheMatchStillAndGivesTheMenuFocus()
        {
            var start = StartMatch();
            while (start.MoveNext())
            {
                yield return start.Current;
            }

            var runner = Object.FindFirstObjectByType<MatchRunner>()!;
            var pause = Object.FindFirstObjectByType<PauseController>();

            Assert.That(pause, Is.Not.Null, "the match has no way to be paused");

            pause!.TogglePause();
            yield return null;

            var frozenAt = runner.Simulation!.CurrentTick;
            Assert.That(runner.IsPaused, Is.True);

            // Focus matters as much as the freeze: without a selection a pad cannot press Resume.
            Assert.That(EventSystem.current!.currentSelectedGameObject, Is.Not.Null,
                "the pause menu must take focus or a gamepad is trapped in it");

            for (var i = 0; i < 60; i++)
            {
                yield return null;
            }

            Assert.That(runner.Simulation!.CurrentTick, Is.EqualTo(frozenAt),
                "a paused match must not advance");

            pause.TogglePause();
            yield return null;

            Assert.That(runner.IsPaused, Is.False);

            var afterResumeSelection = EventSystem.current!.currentSelectedGameObject;
            Assert.That(afterResumeSelection == null || !afterResumeSelection.activeInHierarchy, Is.True,
                "resuming must give up selection; on a pad Submit and Bomb are the same button");

            for (var i = 0; i < 30; i++)
            {
                yield return null;
            }

            Assert.That(runner.Simulation!.CurrentTick, Is.GreaterThan(frozenAt),
                "the match must carry on after resuming");
        }

        [UnityTest]
        public IEnumerator Resuming_DoesNotLurchForwardToCatchUp()
        {
            // Pause skips the accumulator rather than zeroing a timescale, so no backlog builds up.
            // Were it to accumulate, the world would jump the instant the menu closed.
            var start = StartMatch();
            while (start.MoveNext())
            {
                yield return start.Current;
            }

            var runner = Object.FindFirstObjectByType<MatchRunner>()!;
            var pause = Object.FindFirstObjectByType<PauseController>()!;

            pause.TogglePause();

            for (var i = 0; i < 120; i++)
            {
                yield return null;
            }

            pause.TogglePause();
            yield return null;

            Assert.That(runner.TicksLastFrame, Is.LessThanOrEqualTo(MatchRunner.MaxCatchUpTicks));

            var afterResume = runner.Simulation!.CurrentTick;
            yield return null;

            Assert.That(runner.Simulation!.CurrentTick - afterResume,
                Is.LessThanOrEqualTo(MatchRunner.MaxCatchUpTicks),
                "two seconds paused must not become a burst of ticks on resume");
        }

        /// <summary>Boots the game and runs a match, leaving it ticking.</summary>
        private static IEnumerator StartMatch()
        {
            yield return SceneManager.LoadSceneAsync(
                SceneService.NameOf(SceneId.Bootstrap), LoadSceneMode.Single);

            var hub = yield_WaitForHub();
            while (hub.MoveNext())
            {
                yield return hub.Current;
            }

            var graph = Object.FindFirstObjectByType<GameBootstrap>()?.Context;
            Assert.That(graph, Is.Not.Null, "start-up never completed");

            _ = graph!.Scenes.TransitionToAsync(SceneId.Match);

            var frames = 0;
            while (frames < TimeoutFrames)
            {
                var runner = Object.FindFirstObjectByType<MatchRunner>();
                if (runner != null && runner.Simulation != null)
                {
                    yield break;
                }

                frames++;
                yield return null;
            }

            Assert.Fail("the match never started");
        }

        private static IEnumerator yield_WaitForHub()
        {
            var frames = 0;
            while (frames < TimeoutFrames)
            {
                var context = Object.FindFirstObjectByType<GameBootstrap>()?.Context;
                if (context != null && !context.Scenes.IsTransitioning &&
                    context.Scenes.Current == SceneId.Hub)
                {
                    yield break;
                }

                frames++;
                yield return null;
            }

            Assert.Fail("start-up did not reach the hub");
        }
    }
}
