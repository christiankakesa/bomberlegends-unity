using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BomberLegends.Tests.PlayMode
{
    /// <summary>
    /// Verifies the PlayMode test harness itself: that play mode is entered, frames advance,
    /// and the engine-free simulation assembly is loadable at runtime rather than Editor-only.
    /// Integration tests for the bootstrap flow arrive with T-006.
    /// </summary>
    public sealed class PlayModeHarnessTests
    {
        [UnityTest]
        public IEnumerator PlayMode_AdvancesFrames_AndSimulationAssemblyLoads()
        {
            var simulation = Assembly.Load("BomberLegends.Simulation");
            Assert.That(simulation, Is.Not.Null, "The simulation assembly must be available in a runtime domain.");

            var frameBefore = Time.frameCount;
            yield return null;

            Assert.That(Time.frameCount, Is.GreaterThan(frameBefore), "Play mode did not advance a frame.");
        }
    }
}
