using BomberLegends.Core;
using UnityEngine;

namespace BomberLegends.Data.Audio
{
    /// <summary>
    /// A playable sound effect: its clips, its mixer bus, and the limits that stop it flooding the
    /// mixer.
    /// </summary>
    /// <remarks>
    /// The concurrency limits are not polish. A chain detonation can destroy a dozen blocks on a
    /// single tick; without <see cref="MaxConcurrent"/> and <see cref="MinRetriggerInterval"/> that
    /// fires a dozen identical one-shots in one frame, which clips, distorts, and spikes the CPU.
    /// </remarks>
    [CreateAssetMenu(menuName = "Bomber Legends/Audio/Sfx Definition", fileName = "Sfx_")]
    public sealed class SfxDefinition : ScriptableObject
    {
        [Header("Clips")]
        [SerializeField]
        [Tooltip("One clip is chosen at random each time. Several variants stop the sound becoming fatiguing.")]
        private AudioClip[] _clips = System.Array.Empty<AudioClip>();

        [Header("Routing")]
        [SerializeField]
        [Tooltip("Mixer bus this effect is routed through.")]
        private AudioBus _bus = AudioBus.Sfx;

        [Header("Level")]
        [SerializeField, Range(0f, 1f)]
        [Tooltip("Linear volume applied on top of the bus level.")]
        private float _volume = 1f;

        [SerializeField, Range(0f, 0.5f)]
        [Tooltip("Random pitch offset, plus or minus. Zero plays every instance at the same pitch.")]
        private float _pitchVariation = 0.05f;

        [Header("Limiting")]
        [SerializeField, Min(1)]
        [Tooltip("Maximum instances of this effect audible at once. Excess requests are dropped.")]
        private int _maxConcurrent = 4;

        [SerializeField, Min(0f)]
        [Tooltip("Minimum seconds between two instances. Guards against same-tick chain detonations.")]
        private float _minRetriggerInterval = 0.03f;

        /// <summary>The available clip variants. May be empty, in which case nothing plays.</summary>
        public AudioClip[] Clips => _clips;

        /// <summary>The mixer bus this effect is routed through.</summary>
        public AudioBus Bus => _bus;

        /// <summary>Linear volume applied on top of the bus level.</summary>
        public float Volume => _volume;

        /// <summary>Random pitch offset applied per instance, plus or minus.</summary>
        public float PitchVariation => _pitchVariation;

        /// <summary>Maximum instances audible at once.</summary>
        public int MaxConcurrent => _maxConcurrent;

        /// <summary>Minimum seconds between two instances of this effect.</summary>
        public float MinRetriggerInterval => _minRetriggerInterval;

        /// <summary>
        /// Fills in a definition built at run time.
        /// </summary>
        /// <remarks>
        /// The greybox generates its sounds rather than shipping clips, so the definitions wrapping
        /// them cannot be authored in the Inspector. Authored assets are untouched by this — it only
        /// exists so a generated effect passes through exactly the same limiting as a real one.
        /// </remarks>
        public void Configure(
            AudioClip[] clips,
            AudioBus bus,
            float volume,
            float pitchVariation,
            int maxConcurrent,
            float minRetriggerInterval)
        {
            _clips = clips ?? System.Array.Empty<AudioClip>();
            _bus = bus;
            _volume = volume;
            _pitchVariation = pitchVariation;
            _maxConcurrent = System.Math.Max(1, maxConcurrent);
            _minRetriggerInterval = System.Math.Max(0f, minRetriggerInterval);
        }
    }
}
