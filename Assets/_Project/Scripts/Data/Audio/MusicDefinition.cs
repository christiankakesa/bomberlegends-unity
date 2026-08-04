using BomberLegends.Core;
using UnityEngine;

namespace BomberLegends.Data.Audio
{
    /// <summary>
    /// A music track and how it should be introduced.
    /// </summary>
    /// <remarks>
    /// Music clips are the largest single audio asset class, so they are imported with a streaming
    /// load type rather than decompressed into memory.
    /// </remarks>
    [CreateAssetMenu(menuName = "Bomber Legends/Audio/Music Definition", fileName = "Music_")]
    public sealed class MusicDefinition : ScriptableObject
    {
        [Header("Clip")]
        [SerializeField]
        [Tooltip("Import this clip with Load Type set to Streaming.")]
        private AudioClip? _clip;

        [Header("Routing")]
        [SerializeField]
        [Tooltip("Mixer bus this track is routed through.")]
        private AudioBus _bus = AudioBus.Music;

        [Header("Playback")]
        [SerializeField, Range(0f, 1f)]
        [Tooltip("Linear volume applied on top of the bus level.")]
        private float _volume = 1f;

        [SerializeField]
        [Tooltip("Whether the track repeats when it reaches the end.")]
        private bool _loop = true;

        [SerializeField, Min(0f)]
        [Tooltip("Seconds taken to fade this track in when it starts.")]
        private float _fadeInSeconds = 1f;

        /// <summary>The audio clip, or <see langword="null"/> if the asset is not yet authored.</summary>
        public AudioClip? Clip => _clip;

        /// <summary>The mixer bus this track is routed through.</summary>
        public AudioBus Bus => _bus;

        /// <summary>Linear volume applied on top of the bus level.</summary>
        public float Volume => _volume;

        /// <summary>Whether the track repeats when it reaches the end.</summary>
        public bool Loop => _loop;

        /// <summary>Seconds taken to fade this track in when it starts.</summary>
        public float FadeInSeconds => _fadeInSeconds;
    }
}
