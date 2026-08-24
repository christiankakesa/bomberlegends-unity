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

        /// <summary>
        /// The level every generated clip is brought to, measured the way a phone hears it.
        /// </summary>
        /// <remarks>
        /// Chosen as the loudest the least co-operative clip reaches without the peak ceiling
        /// biting. Every clip landing on the same number is the point: it means an
        /// <c>SfxDefinition</c>'s volume says what a designer meant rather than compensating for
        /// however loud a synthesis function happened to come out.
        /// </remarks>
        private const float TargetLoudness = 0.07f;

        /// <summary>
        /// Where a small speaker stops reproducing anything, near enough.
        /// </summary>
        /// <remarks>
        /// Loudness is measured above this rather than across the whole spectrum, because energy
        /// below it does not reach the player on the primary target platform. Judging these clips
        /// by their raw level is what let the bomb drop sit three times quieter than the dash on a
        /// Seeker 2 while both looked correct on a desk.
        /// </remarks>
        private const float SpeakerFloorHertz = 400f;

        /// <summary>Loudest sample allowed, leaving headroom rather than touching full scale.</summary>
        private const float PeakCeiling = 0.95f;

        /// <summary>
        /// A knock with a bright edge on it: something has been put down.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Was a bare sine sweeping 150 Hz to 80 Hz, which is a good bomb on a desk and no bomb at
        /// all on a phone: the drivers in the three test devices move almost no air below about
        /// 400 Hz, so the one sound the player makes most often was the one they could not hear.
        /// </para>
        /// <para>
        /// Rebuilt in three layers instead. The body carries the pitch for anything with a speaker
        /// worth the name; its second and third harmonics sit where a phone can actually reproduce
        /// them, and the ear reconstructs the missing fundamental from those, so the knock keeps its
        /// depth on a driver that cannot produce any. The short tap on top is most of what survives
        /// a small speaker, and it is what makes this read as something set down rather than a hum.
        /// </para>
        /// <para>
        /// The measurable version of all that: run the clip through a 300 Hz high-pass and it keeps
        /// three quarters of its level, against the old one's two fifths.
        /// </para>
        /// </remarks>
        public static AudioClip Thump() => Build("sfx_thump", 0.18f, (t, span) =>
        {
            var body = Sine(t, Sweep(t, span, 300f, 150f)) * Decay(t, span, 6f);

            var harmonics =
                Sine(t, Sweep(t, span, 600f, 300f)) * 0.9f * Decay(t, span, 8f) +
                Sine(t, Sweep(t, span, 900f, 450f)) * 0.55f * Decay(t, span, 11f);

            var tap = Noise(t) * Decay(t, span, 80f) * 0.5f;

            return body + harmonics + tap;
        });

        /// <summary>
        /// A heavy burst with a low body: an explosion.
        /// </summary>
        /// <remarks>
        /// Had the same fault the bomb drop did — a 90 Hz body a phone cannot move, leaving the
        /// noise on top to carry the whole thing, so the game's payoff arrived as a hiss. It gains
        /// a mid layer for the same reason the knock did.
        /// <para>
        /// The saturation is doing real work rather than decorating. An explosion has an enormous
        /// crest factor, and normalising one by loudness runs straight into the peak ceiling: this
        /// clip could not reach the level of any other however hard it was driven. Rounding the
        /// transient off buys the level back, and adds harmonics while it is there — which is what
        /// saturation is for on an explosion anyway. It also replaces the hard clamp the old
        /// version was hitting on every play, which is the ugly way of doing the same thing.
        /// </para>
        /// </remarks>
        public static AudioClip Boom() => Build("sfx_boom", 0.55f, (t, span) =>
        {
            var body = Sine(t, Sweep(t, span, 90f, 40f)) * 0.5f;

            var mid =
                Sine(t, Sweep(t, span, 220f, 100f)) * 0.5f * Decay(t, span, 6f) +
                Sine(t, Sweep(t, span, 380f, 170f)) * 0.3f * Decay(t, span, 9f);

            var grit = Noise(t) * Decay(t, span, 9f) * 0.6f;

            return Saturate((body + mid + grit) * 1.6f) * Decay(t, span, 4.5f);
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

        /// <summary>
        /// Builds a mono clip from a sample function, at the same loudness as every other.
        /// </summary>
        /// <remarks>
        /// A sample function writes whatever level falls out of its layers, and is meant to: it
        /// describes a <i>shape</i>. Levelling here is what lets the feedback table's volumes be a
        /// statement about what matters — the hurt sound above the block breaking — instead of a
        /// per-clip correction nobody can read as intent.
        /// </remarks>
        private static AudioClip Build(string name, float seconds, Func<float, float, float> sample)
        {
            var count = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            var data = new float[count];

            for (var i = 0; i < count; i++)
            {
                data[i] = sample((float)i / SampleRate, seconds);
            }

            // Short fade at both ends. A waveform cut mid-cycle produces an audible click, which is
            // the single most common thing that makes generated audio sound broken rather than cheap.
            Taper(data);
            Normalise(data);

            var clip = AudioClip.Create(name, count, 1, SampleRate, stream: false);
            clip.SetData(data, 0);

            return clip;
        }

        /// <summary>
        /// Scales the clip to <see cref="TargetLoudness"/> as heard through a small speaker, unless
        /// its peak would reach the ceiling first.
        /// </summary>
        /// <remarks>
        /// Two clips at the same loudness are the same loudness to a listener; two clips at the same
        /// peak are not, which is why the peak is a limit here rather than the goal. A clip whose
        /// crest factor stops it reaching the target lands under it, and the only honest answer for
        /// that one is to give its own sample function less transient to fit around — which is what
        /// the saturation in <see cref="Boom"/> is doing.
        /// </remarks>
        private static void Normalise(float[] data)
        {
            var loudness = LoudnessAboveSpeakerFloor(data);
            if (loudness <= 0f)
            {
                return;
            }

            var peak = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }

            var gain = TargetLoudness / loudness;
            if (peak > 0f)
            {
                gain = Mathf.Min(gain, PeakCeiling / peak);
            }

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = Mathf.Clamp(data[i] * gain, -1f, 1f);
            }
        }

        /// <summary>
        /// How loud the clip is once everything a phone cannot reproduce is taken out of it.
        /// </summary>
        /// <remarks>
        /// A one-pole high-pass at <see cref="SpeakerFloorHertz"/> and the root mean square of what
        /// survives. Far cruder than any real driver's roll-off, and meant to be: the question is
        /// only "how much of this can a small speaker move", and a sharper filter would imply a
        /// precision the answer does not have. Runs as a single streaming pass, so measuring a clip
        /// costs no memory beyond the clip.
        /// </remarks>
        private static float LoudnessAboveSpeakerFloor(float[] data)
        {
            var interval = 1f / SampleRate;
            var constant = 1f / (2f * Mathf.PI * SpeakerFloorHertz);
            var alpha = constant / (constant + interval);

            var filtered = 0f;
            var previous = 0f;
            var total = 0.0;

            for (var i = 0; i < data.Length; i++)
            {
                filtered = alpha * (filtered + data[i] - previous);
                previous = data[i];
                total += (double)filtered * filtered;
            }

            return Mathf.Sqrt((float)(total / Mathf.Max(1, data.Length)));
        }

        /// <summary>Rounds a signal off towards full scale instead of cutting it there.</summary>
        private static float Saturate(float value) => (float)Math.Tanh(value);

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
