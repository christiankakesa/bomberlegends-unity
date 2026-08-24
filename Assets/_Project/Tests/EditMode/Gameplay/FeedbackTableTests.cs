using BomberLegends.Data.Audio;
using BomberLegends.Simulation.Events;
using NUnit.Framework;
using UnityEngine;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers the table that binds simulation events to sound and camera shake.
    /// </summary>
    public sealed class FeedbackTableTests
    {
        private readonly System.Collections.Generic.List<Object> _created =
            new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        private FeedbackTable Table(params FeedbackEntry[] entries)
        {
            var table = ScriptableObject.CreateInstance<FeedbackTable>();
            table.SetEntries(entries);
            _created.Add(table);

            return table;
        }

        private static FeedbackEntry Entry(SimEventType type, float shake = 0f) => new FeedbackEntry
        {
            Event = type,
            Sfx = null,
            ShakeStrength = shake,
            ShakeSeconds = 0.2f
        };

        [Test]
        public void AnEventWithNoRowProducesNothing()
        {
            // Silence is the correct answer for an unbound event, not an error. Most events will
            // never have feedback, and the view must not care.
            var table = Table(Entry(SimEventType.BombPlaced));

            Assert.That(table.TryGet(SimEventType.ArenaCleared, out _), Is.False);
        }

        [Test]
        public void AnEventWithARowReturnsIt()
        {
            var table = Table(Entry(SimEventType.PlayerDied, shake: 0.9f));

            Assert.That(table.TryGet(SimEventType.PlayerDied, out var entry), Is.True);
            Assert.That(entry.ShakeStrength, Is.EqualTo(0.9f));
        }

        [Test]
        public void TheLastRowForAnEventWins()
        {
            // So a designer can override a binding by appending, rather than hunting for the row
            // that already exists.
            var table = Table(
                Entry(SimEventType.BombDetonated, shake: 0.2f),
                Entry(SimEventType.BombDetonated, shake: 0.8f));

            table.TryGet(SimEventType.BombDetonated, out var entry);

            Assert.That(entry.ShakeStrength, Is.EqualTo(0.8f));
        }

        [Test]
        public void ReplacingTheRowsIsReflectedImmediately()
        {
            // The lookup is cached, so a table edited at run time has to invalidate it — otherwise
            // tuning feel while the game runs would silently do nothing.
            var table = Table(Entry(SimEventType.BombPlaced));

            table.SetEntries(new[] { Entry(SimEventType.EnemyKilled) });

            Assert.That(table.TryGet(SimEventType.BombPlaced, out _), Is.False);
            Assert.That(table.TryGet(SimEventType.EnemyKilled, out _), Is.True);
        }

        // ---------- the generated placeholder set ----------

        [Test]
        public void ThePlaceholderTableCoversEveryMomentThatCarriesTheLoop()
        {
            var table = PlaceholderFeedback.CreateTable();
            _created.Add(table);

            var required = new[]
            {
                SimEventType.BombPlaced,
                SimEventType.BombDetonated,
                SimEventType.BlockDestroyed,
                SimEventType.EnemyKilled,
                SimEventType.DamageTaken,
                SimEventType.PlayerDied
            };

            foreach (var type in required)
            {
                Assert.That(table.TryGet(type, out var entry), Is.True, $"{type} has no feedback");
                Assert.That(entry.Sfx, Is.Not.Null, $"{type} has no sound");
                Assert.That(entry.Sfx!.Clips.Length, Is.GreaterThan(0), $"{type} has no clip");
            }
        }

        [Test]
        public void BeingHurtIsTheLoudestThingInTheGame()
        {
            // Two of the five gate metrics depend on a player knowing what killed them. If this
            // ever stops being unmistakable, the playtest measures the controls instead.
            var table = PlaceholderFeedback.CreateTable();
            _created.Add(table);

            table.TryGet(SimEventType.DamageTaken, out var hurt);
            table.TryGet(SimEventType.BlockDestroyed, out var block);
            table.TryGet(SimEventType.PlayerDied, out var died);

            Assert.That(hurt.Sfx!.Volume, Is.GreaterThan(block.Sfx!.Volume));
            Assert.That(hurt.ShakeStrength, Is.GreaterThan(block.ShakeStrength));
            Assert.That(died.ShakeStrength, Is.GreaterThanOrEqualTo(hurt.ShakeStrength));
        }

        [Test]
        public void BlastAudioIsBoundToTheDetonationNotToItsTiles()
        {
            // One chain lights a hundred tiles on a single tick. A sound per tile is not an
            // explosion, it is a burst of noise and a CPU spike at the worst possible moment.
            var table = PlaceholderFeedback.CreateTable();
            _created.Add(table);

            Assert.That(table.TryGet(SimEventType.BlastSpawned, out _), Is.False);
            Assert.That(table.TryGet(SimEventType.BombDetonated, out _), Is.True);
        }

        [Test]
        public void EveryPlaceholderSoundIsVoiceLimited()
        {
            var table = PlaceholderFeedback.CreateTable();
            _created.Add(table);

            foreach (var entry in table.Entries)
            {
                if (entry.Sfx == null)
                {
                    continue;
                }

                Assert.That(entry.Sfx.MaxConcurrent, Is.GreaterThan(0));
                Assert.That(entry.Sfx.MaxConcurrent, Is.LessThanOrEqualTo(6),
                    $"{entry.Event} may flood the mixer during a chain");
            }
        }

        [Test]
        public void TheBombGoingDownSurvivesAPhoneSpeaker()
        {
            // The sound the player makes more often than any other, on hardware that moves almost
            // no air below 400 Hz. A clip whose level collapses under a 300 Hz high-pass is one the
            // player on the primary target platform cannot hear at all.
            var clip = ProceduralClips.Thump();

            var data = new float[clip.samples];
            clip.GetData(data, 0);

            var kept = Rms(HighPassed(data, 300f)) / Rms(data);

            Assert.That(kept, Is.GreaterThan(0.6f),
                $"only {kept:P0} of the bomb-drop survives a 300 Hz high-pass; a phone plays what is left");

            Object.DestroyImmediate(clip);
        }

        /// <summary>
        /// The signal with everything below <paramref name="hertz"/> taken out of it.
        /// </summary>
        /// <remarks>
        /// A one-pole high-pass, which is far cruder than any real speaker's roll-off and is meant
        /// to be: it stands in for the question "is there anything here a small driver can move?"
        /// and nothing more.
        /// </remarks>
        private static float[] HighPassed(float[] data, float hertz)
        {
            const float SampleRate = 44100f;

            var interval = 1f / SampleRate;
            var constant = 1f / (2f * Mathf.PI * hertz);
            var alpha = constant / (constant + interval);

            var output = new float[data.Length];

            for (var i = 1; i < data.Length; i++)
            {
                output[i] = alpha * (output[i - 1] + data[i] - data[i - 1]);
            }

            return output;
        }

        private static float Rms(float[] data)
        {
            var total = 0.0;

            for (var i = 0; i < data.Length; i++)
            {
                total += (double)data[i] * data[i];
            }

            return Mathf.Sqrt((float)(total / Mathf.Max(1, data.Length)));
        }

        [Test]
        public void GeneratedClipsAreAudibleRatherThanEmpty()
        {
            var clip = ProceduralClips.Hurt();

            Assert.That(clip.samples, Is.GreaterThan(1000));
            Assert.That(clip.channels, Is.EqualTo(1));

            var data = new float[clip.samples];
            clip.GetData(data, 0);

            var peak = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }

            Assert.That(peak, Is.GreaterThan(0.1f), "a generated sound that is silent is not a sound");
            Assert.That(peak, Is.LessThanOrEqualTo(1f), "and one that clips is worse than none");

            Object.DestroyImmediate(clip);
        }
    }
}
