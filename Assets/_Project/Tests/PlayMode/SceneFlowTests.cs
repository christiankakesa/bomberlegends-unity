using System.Collections;
using BomberLegends.Bootstrap;
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
    /// Drives the real bootstrap and scene service through a full hub–match–hub cycle.
    /// </summary>
    /// <remarks>
    /// The object lookups here would be banned in gameplay code; in an integration test they are the
    /// only way to inspect what the scenes actually produced, and they run once rather than per frame.
    /// </remarks>
    public sealed class SceneFlowTests
    {
        private const int TimeoutFrames = 900;

        [UnityTest]
        public IEnumerator Bootstrap_StartsUpAndOpensTheHub()
        {
            yield return LoadBootstrap();

            var context = GetContext();
            Assert.That(context, Is.Not.Null, "the service graph was never composed");
            Assert.That(context!.Scenes.Current, Is.EqualTo(SceneId.Hub));
            Assert.That(context.Save.Data, Is.Not.Null, "the save should have been loaded during start-up");
        }

        [UnityTest]
        public IEnumerator Bootstrap_LeavesExactlyOneAudioListenerAndEventSystem()
        {
            yield return LoadBootstrap();

            AssertSingletons();
        }

        [UnityTest]
        public IEnumerator SceneFlow_HubToMatchToHub_RestoresTheStartingObjectCount()
        {
            yield return LoadBootstrap();

            var context = GetContext()!;
            var rootsInHub = CountLoadedRootObjects();

            yield return TransitionTo(context, SceneId.Match);
            Assert.That(context.Scenes.Current, Is.EqualTo(SceneId.Match));
            AssertSingletons();

            yield return TransitionTo(context, SceneId.Hub);
            Assert.That(context.Scenes.Current, Is.EqualTo(SceneId.Hub));
            AssertSingletons();

            Assert.That(CountLoadedRootObjects(), Is.EqualTo(rootsInHub),
                "a round trip left objects behind, so a scene was not fully unloaded");
        }

        [UnityTest]
        public IEnumerator SceneFlow_OnlyOneAdditiveSceneIsLoadedAtATime()
        {
            yield return LoadBootstrap();

            var context = GetContext()!;
            yield return TransitionTo(context, SceneId.Match);

            Assert.That(SceneManager.GetSceneByName(SceneService.NameOf(SceneId.Hub)).isLoaded, Is.False,
                "the hub should have been unloaded before the match was loaded");
            Assert.That(SceneManager.GetSceneByName(SceneService.NameOf(SceneId.Bootstrap)).isLoaded, Is.True,
                "bootstrap is persistent and must never be unloaded");
        }

        [UnityTest]
        public IEnumerator SceneService_RejectsATransitionToBootstrap()
        {
            yield return LoadBootstrap();

            var context = GetContext()!;
            Assert.Throws<System.ArgumentException>(
                () => context.Scenes.TransitionToAsync(SceneId.Bootstrap));
        }

        private static IEnumerator LoadBootstrap()
        {
            yield return SceneManager.LoadSceneAsync(
                SceneService.NameOf(SceneId.Bootstrap), LoadSceneMode.Single);

            var frames = 0;
            while (frames < TimeoutFrames)
            {
                var context = GetContext();
                if (context != null && !context.Scenes.IsTransitioning &&
                    context.Scenes.Current == SceneId.Hub)
                {
                    yield break;
                }

                frames++;
                yield return null;
            }

            Assert.Fail("start-up did not reach the hub within the timeout");
        }

        private static IEnumerator TransitionTo(GameContext context, SceneId target)
        {
            _ = context.Scenes.TransitionToAsync(target);

            var frames = 0;
            while ((context.Scenes.IsTransitioning || context.Scenes.Current != target) &&
                   frames < TimeoutFrames)
            {
                frames++;
                yield return null;
            }

            Assert.That(context.Scenes.Current, Is.EqualTo(target),
                $"the transition to {target} did not complete within the timeout");
        }

        private static GameContext? GetContext() =>
            Object.FindFirstObjectByType<GameBootstrap>()?.Context;

        private static void AssertSingletons()
        {
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,
                Is.EqualTo(1), "there must be exactly one AudioListener, on the bootstrap scene");
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,
                Is.EqualTo(1), "there must be exactly one EventSystem, on the bootstrap scene");
        }

        private static int CountLoadedRootObjects()
        {
            var total = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    total += scene.rootCount;
                }
            }

            return total;
        }
    }
}
