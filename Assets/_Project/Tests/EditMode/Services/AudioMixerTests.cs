using BomberLegends.Core;
using BomberLegends.Services.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace BomberLegends.Tests.EditMode.Services
{
    /// <summary>
    /// Covers the project mixer against the buses the code believes exist.
    /// </summary>
    /// <remarks>
    /// A mixer is authored data, and the only thing binding it to the code is a set of strings: a
    /// group named for a bus and a parameter named for its level. Rename a group in the mixer window
    /// and nothing fails to compile — the sound simply routes to Master and a settings slider stops
    /// working, on device, quietly. These tests are that missing compile error.
    /// </remarks>
    public sealed class AudioMixerTests
    {
        private const string MixerPath = "Assets/_Project/Settings/Audio/MainMixer.mixer";

        private static AudioMixer Mixer
        {
            get
            {
                var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
                Assert.That(mixer, Is.Not.Null, $"the project mixer is missing from {MixerPath}");

                return mixer!;
            }
        }

        [Test]
        public void TheMixerHasAGroupForEveryBusTheCodeKnowsAbout()
        {
            var groups = Mixer.FindMatchingGroups(string.Empty);

            foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
            {
                var name = bus.ToString();
                var found = false;

                for (var i = 0; i < groups.Length; i++)
                {
                    found |= groups[i] != null && groups[i].name == name;
                }

                Assert.That(found, Is.True, $"the mixer has no '{name}' group to route that bus to");
            }
        }

        [Test]
        public void EveryBusButMasterHangsBeneathMaster()
        {
            // The whole reason for using a mixer rather than multiplying volumes: lowering the root
            // has to lower everything under it, and that is true of a graph and not of an array.
            foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
            {
                if (bus == AudioBus.Master)
                {
                    continue;
                }

                Assert.That(Mixer.FindMatchingGroups($"Master/{bus}"), Is.Not.Empty,
                    $"{bus} does not sit under Master, so the master level will not reach it");
            }
        }

        [Test]
        public void EveryBusLevelIsExposedForTheSettingsScreen()
        {
            // Read from the asset rather than through GetFloat, which answers about the running
            // mixer. What matters here is what was authored.
            var exposed = new SerializedObject(Mixer).FindProperty("m_ExposedParameters");
            Assert.That(exposed, Is.Not.Null, "the mixer exposes no parameters at all");

            var names = new string[exposed!.arraySize];
            for (var i = 0; i < exposed.arraySize; i++)
            {
                names[i] = exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
            }

            foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
            {
                var parameter = AudioService.ParameterFor(bus);

                Assert.That(names, Does.Contain(parameter),
                    $"'{parameter}' is not exposed, so the {bus} slider would do nothing");
            }
        }

        [Test]
        public void SilenceIsTheMixersFloorAndFullVolumeChangesNothing()
        {
            // Mixer levels are decibels. Zero means untouched, not silent, and a linear half is
            // about six decibels down rather than half as loud.
            Assert.That(AudioService.Decibels(1f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(AudioService.Decibels(0.5f), Is.EqualTo(-6.02f).Within(0.05f));
            Assert.That(AudioService.Decibels(0f), Is.EqualTo(-80f).Within(0.01f),
                "silence must land on the mixer's own suspend threshold, not minus infinity");
        }

        [Test]
        public void WithoutAMixerTheMasterBusStillReachesEverySound()
        {
            // What the graph does for free, done by hand. The version before the mixer applied one
            // bus per sound, so turning Master down quietened the music and nothing else.
            Assert.That(AudioService.CombinedLevel(AudioBus.Sfx, 0.5f, 0.5f),
                Is.EqualTo(0.25f).Within(0.001f));

            Assert.That(AudioService.CombinedLevel(AudioBus.Sfx, 1f, 0f), Is.EqualTo(0f),
                "a silent master must silence the effects bus too");

            Assert.That(AudioService.CombinedLevel(AudioBus.Master, 0.5f, 0.5f),
                Is.EqualTo(0.5f).Within(0.001f),
                "a sound already on Master must not be attenuated by it twice");
        }

        [Test]
        public void BusLevelsSurviveBeingSetAndRead()
        {
            var host = new GameObject("Audio");

            try
            {
                var audio = new AudioService(host.transform, voices: 2);

                audio.SetBusVolume(AudioBus.Sfx, 0.25f);

                Assert.That(audio.GetBusVolume(AudioBus.Sfx), Is.EqualTo(0.25f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
