using System.Collections;
using BomberLegends.Bootstrap;
using BomberLegends.Core;
using BomberLegends.Gameplay.Match;
using BomberLegends.Services;
using BomberLegends.Services.Scenes;
using NUnit.Framework;
using UnityEngine;
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
