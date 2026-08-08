using System;
using UnityEngine;

namespace BomberLegends.Data.Audio
{
    /// <summary>
    /// Synthesises the greybox's placeholder sounds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The slice ships with no authored audio, for the same reason it ships with no authored art:
    /// content produced against unvalidated mechanics is content thrown away. But silence is not a
    /// neutral placeholder — a player who cannot hear that they were hurt reports the controls
    /// killed them, which corrupts the very measurement the slice exists to take.
    /// </para>
    /// <para>
    /// So the sounds are generated. They are crude on purpose and exist to make each moment
    /// <i>distinguishable</i>, not pleasant. Every one is reachable through the same
    /// <c>SfxDefinition</c> a real clip would use, so replacing them later changes an asset and
    /// nothing else.
    /// </para>
    /// </remarks>
    public static class ProceduralClips
    {
        private const int SampleRate = 44100;

        /// <summary>A dull low knock: something has been put down.</summary>
        public static AudioClip Thump() => Build("sfx_thump", 0.14f, (t, span) =>
            Sine(t, Sweep(t, span, 150f, 80f)) * Decay(t, span, 14f));

        /// <summary>A heavy burst with a low body: an explosion.</summary>
        public static AudioClip Boom() => Build("sfx_boom", 0.55f, (t, span) =>
        {
            var body = Sine(t, Sweep(t, span, 90f, 40f)) * 0.8f;
            var grit = Noise(t) * Decay(t, span, 9f) * 0.6f;
            return (body + grit) * Decay(t, span, 4.5f);
        });

        /// <summary>A short bright crack: something broke.</summary>
        public static AudioClip Crunch() => Build("sfx_crunch", 0.18f, (t, span) =>
            (Noise(t) * 0.9f + Sine(t, 220f) * 0.3f) * Decay(t, span, 22f));

        /// <summary>A harsh falling tone. Deliberately unpleasant: this one must never be missed.</summary>
        public static AudioClip Hurt() => Build("sfx_hurt", 0.30f, (t, span) =>
            Square(t, Sweep(t, span, 420f, 140f)) * 0.55f * Decay(t, span, 8f));

        /// <summary>A long descent: the run is over.</summary>
        public static AudioClip Death() => Build("sfx_death", 1.10f, (t, span) =>
            Sine(t, Sweep(t, span, 320f, 55f)) * Decay(t, span, 2.2f));

        /// <summary>A short falling blip: something died. Distinct from a block breaking.</summary>
        public static AudioClip Pop() => Build("sfx_pop", 0.18f, (t, span) =>
            Sine(t, Sweep(t, span, 700f, 260f)) * 0.5f * Decay(t, span, 11f));

        /// <summary>Air moving: a dash.</summary>
        public static AudioClip Whoosh() => Build("sfx_whoosh", 0.26f, (t, span) =>
        {
            var shape = Mathf.Sin(Mathf.PI * (t / span));
            return Noise(t) * shape * shape * 0.5f;
        });

        /// <summary>A tight rising blip: something was fired.</summary>
        public static AudioClip Shot() => Build("sfx_shot", 0.10f, (t, span) =>
            Sine(t, Sweep(t, span, 620f, 940f)) * 0.5f * Decay(t, span, 20f));

        /// <summary>Two rising tones: something was gained.</summary>
        public static AudioClip Pickup() => Build("sfx_pickup", 0.34f, (t, span) =>
        {
            var step = t < span * 0.45f ? 520f : 780f;
            return Sine(t, step) * 0.45f * Decay(t, span, 5f);
        });

        /// <summary>Three ascending tones: the arena is clear.</summary>
        public static AudioClip Fanfare() => Build("sfx_fanfare", 0.70f, (t, span) =>
        {
            var third = span / 3f;
            var step = t < third ? 440f : t < third * 2f ? 550f : 660f;
            return Sine(t, step) * 0.4f * Decay(t, span, 2.6f);
        });

        /// <summary>Builds a mono clip from a sample function.</summary>
        private static AudioClip Build(string name, float seconds, Func<float, float, float> sample)
        {
            var count = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = (float)i / SampleRate;
                data[i] = Mathf.Clamp(sample(t, seconds), -1f, 1f);
            }

            // Short fade at both ends. A waveform cut mid-cycle produces an audible click, which is
            // the single most common thing that makes generated audio sound broken rather than cheap.
            Taper(data);

            var clip = AudioClip.Create(name, count, 1, SampleRate, stream: false);
            clip.SetData(data, 0);

            return clip;
        }

        private static void Taper(float[] data)
        {
            var edge = Mathf.Min(256, data.Length / 8);

            for (var i = 0; i < edge; i++)
            {
                var gain = (float)i / edge;
                data[i] *= gain;
                data[data.Length - 1 - i] *= gain;
            }
        }

        private static float Sine(float t, float hertz) => Mathf.Sin(2f * Mathf.PI * hertz * t);

        private static float Square(float t, float hertz) => Mathf.Sign(Sine(t, hertz));

        /// <summary>Value noise from a hash, so a build always produces identical audio.</summary>
        private static float Noise(float t)
        {
            var n = (uint)(t * SampleRate);
            n ^= n << 13;
            n ^= n >> 17;
            n ^= n << 5;

            return ((n & 0xFFFF) / 32768f) - 1f;
        }

        private static float Sweep(float t, float span, float from, float to) =>
            Mathf.Lerp(from, to, span <= 0f ? 0f : t / span);

        private static float Decay(float t, float span, float rate) =>
            span <= 0f ? 0f : Mathf.Exp(-rate * (t / span));
    }
}
