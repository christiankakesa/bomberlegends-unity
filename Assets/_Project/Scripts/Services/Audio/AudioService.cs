using System.Collections.Generic;
using BomberLegends.Core;
using BomberLegends.Data.Audio;
using UnityEngine;

namespace BomberLegends.Services.Audio
{
    /// <summary>
    /// Plays sound through a pool of sources, enforcing the limits each effect declares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The concurrency rules are the reason this class exists rather than callers touching
    /// <c>AudioSource</c> directly. A chain detonation can destroy a dozen blocks and light a
    /// hundred tiles on a single tick; playing one sound per event would clip, distort, and spike
    /// the CPU at the exact moment the game is at its most demanding.
    /// </para>
    /// <para>
    /// Bus levels are applied as plain multipliers rather than through an <c>AudioMixer</c>, because
    /// no mixer asset exists yet. The interface does not expose the difference, so introducing one
    /// later changes this class and nothing that calls it.
    /// </para>
    /// </remarks>
    public sealed class AudioService : IAudioService
    {
        private const int BusCount = 6;

        private readonly AudioSource[] _sources;
        private readonly float[] _busVolumes = new float[BusCount];
        private readonly Dictionary<SfxDefinition, float> _lastPlayed = new Dictionary<SfxDefinition, float>();
        private readonly Dictionary<SfxDefinition, int> _live = new Dictionary<SfxDefinition, int>();
        private readonly SfxDefinition?[] _playing;
        private readonly float[] _freeAt;

        private AudioSource? _music;
        private MusicDefinition? _currentTrack;
        private uint _pitchSeed = 0x9E3779B9u;

        /// <summary>Creates the service and its pool beneath the given transform.</summary>
        public AudioService(Transform parent, int voices = 16)
        {
            for (var i = 0; i < BusCount; i++)
            {
                _busVolumes[i] = 1f;
            }

            _sources = new AudioSource[voices];
            _playing = new SfxDefinition?[voices];
            _freeAt = new float[voices];

            for (var i = 0; i < voices; i++)
            {
                var host = new GameObject($"Voice {i}");
                host.transform.SetParent(parent, false);

                var source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;

                _sources[i] = source;
            }

            var musicHost = new GameObject("Music");
            musicHost.transform.SetParent(parent, false);

            _music = musicHost.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
        }

        /// <summary>
        /// Plays a one-shot effect.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <paramref name="worldPosition"/> is accepted and currently ignored, and that is a fix
        /// rather than an oversight. The only <c>AudioListener</c> lives on the bootstrap object so
        /// that exactly one exists and it is never unloaded — which also means it sits at the world
        /// origin and never moves. Sounds play at tile coordinates, so an arena spanning twenty
        /// units made every event quieter the further it happened from one corner of the board, for
        /// no reason a player could perceive as anything but inconsistency.
        /// </para>
        /// <para>
        /// Flat playback is also simply correct for this camera: the whole arena is on screen at a
        /// near-constant distance, so distance attenuation models nothing real. Stereo panning would
        /// be worth having and is not the same thing; it needs a position relative to the view, not
        /// to a listener parked at the origin.
        /// </para>
        /// </remarks>
        public void PlaySfx(SfxDefinition definition, Vector3? worldPosition = null)
        {
            if (definition == null || definition.Clips == null || definition.Clips.Length == 0)
            {
                return;
            }

            Retire();

            if (!MayPlay(definition))
            {
                return;
            }

            var voice = TakeVoice();
            if (voice < 0)
            {
                return;
            }

            var clip = definition.Clips[NextRandom((uint)definition.Clips.Length)];
            if (clip == null)
            {
                return;
            }

            var source = _sources[voice];

            source.clip = clip;
            source.volume = definition.Volume * _busVolumes[(int)definition.Bus];
            source.pitch = 1f + Spread(definition.PitchVariation);

            // Flat, always. See the note on the position parameter: spatialising against a listener
            // that never moves attenuated every sound by its distance from the world origin.
            source.spatialBlend = 0f;

            source.Play();

            _playing[voice] = definition;
            _freeAt[voice] = Time.unscaledTime + (clip.length / Mathf.Max(0.01f, source.pitch));
            _lastPlayed[definition] = Time.unscaledTime;
            _live[definition] = Count(definition) + 1;
        }

        /// <inheritdoc />
        public void PlayMusic(MusicDefinition definition)
        {
            if (_music == null || definition == null || definition.Clip == null ||
                ReferenceEquals(definition, _currentTrack))
            {
                return;
            }

            _currentTrack = definition;
            _music.clip = definition.Clip;
            _music.volume = definition.Volume * _busVolumes[(int)AudioBus.Music];
            _music.Play();
        }

        /// <inheritdoc />
        public void StopMusic(float fadeSeconds = 1f)
        {
            _currentTrack = null;
            _music?.Stop();
        }

        /// <inheritdoc />
        public void SetBusVolume(AudioBus bus, float normalized01)
        {
            _busVolumes[(int)bus] = Mathf.Clamp01(normalized01);

            if (bus is AudioBus.Music or AudioBus.Master && _music != null && _currentTrack != null)
            {
                _music.volume = _currentTrack.Volume * _busVolumes[(int)AudioBus.Music];
            }
        }

        /// <inheritdoc />
        public float GetBusVolume(AudioBus bus) => _busVolumes[(int)bus];

        /// <inheritdoc />
        public void StopAll()
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                _sources[i].Stop();
                _playing[i] = null;
                _freeAt[i] = 0f;
            }

            _live.Clear();
            _lastPlayed.Clear();
            StopMusic(0f);
        }

        /// <summary>Whether the effect's own limits allow another instance right now.</summary>
        private bool MayPlay(SfxDefinition definition)
        {
            if (_lastPlayed.TryGetValue(definition, out var last) &&
                Time.unscaledTime - last < definition.MinRetriggerInterval)
            {
                return false;
            }

            return Count(definition) < definition.MaxConcurrent;
        }

        private int Count(SfxDefinition definition) =>
            _live.TryGetValue(definition, out var count) ? count : 0;

        /// <summary>Releases voices whose clips have finished.</summary>
        private void Retire()
        {
            var now = Time.unscaledTime;

            for (var i = 0; i < _sources.Length; i++)
            {
                var definition = _playing[i];

                if (definition == null || now < _freeAt[i])
                {
                    continue;
                }

                _live[definition] = Mathf.Max(0, Count(definition) - 1);
                _playing[i] = null;
            }
        }

        /// <summary>
        /// Finds a free voice, or the one closest to finishing.
        /// </summary>
        /// <remarks>
        /// Stealing the oldest rather than dropping the request. Under a chain the per-effect limits
        /// already refuse most of the flood; anything that gets this far is a different sound, and
        /// silence would be the wrong answer for it.
        /// </remarks>
        private int TakeVoice()
        {
            var oldest = -1;
            var oldestFreeAt = float.MaxValue;

            for (var i = 0; i < _sources.Length; i++)
            {
                if (_playing[i] == null)
                {
                    return i;
                }

                if (_freeAt[i] < oldestFreeAt)
                {
                    oldestFreeAt = _freeAt[i];
                    oldest = i;
                }
            }

            if (oldest >= 0)
            {
                var stolen = _playing[oldest];
                if (stolen != null)
                {
                    _live[stolen] = Mathf.Max(0, Count(stolen) - 1);
                }

                _playing[oldest] = null;
            }

            return oldest;
        }

        /// <summary>A pitch offset within the effect's declared range.</summary>
        /// <remarks>
        /// Its own generator, never the simulation's. Drawing from that would make audio settings
        /// capable of changing the outcome of a match.
        /// </remarks>
        private float Spread(float variation)
        {
            if (variation <= 0f)
            {
                return 0f;
            }

            var roll = NextRandom(2000) / 1000f - 1f;
            return roll * variation;
        }

        private uint NextRandom(uint exclusiveMax)
        {
            _pitchSeed ^= _pitchSeed << 13;
            _pitchSeed ^= _pitchSeed >> 17;
            _pitchSeed ^= _pitchSeed << 5;

            return exclusiveMax == 0 ? 0 : _pitchSeed % exclusiveMax;
        }
    }
}
