using System.Collections.Generic;
using BomberLegends.Core;
using BomberLegends.Data.Audio;
using UnityEngine;
using UnityEngine.Audio;

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
    /// Bus levels go through an <c>AudioMixer</c> when one is supplied, and fall back to plain
    /// multipliers when it is not — which is what every test does, and what a scene missing the
    /// reference does rather than falling silent. The interface does not expose the difference.
    /// </para>
    /// <para>
    /// The mixer is not decoration. Buses are a <i>graph</i> there: Master is the parent of the
    /// other five, so lowering it lowers everything by construction. The multiplier fallback cannot
    /// express that — it applies one bus per sound and Master reaches nothing but the music.
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
        private readonly AudioMixer? _mixer;
        private readonly AudioMixerGroup?[] _groups = new AudioMixerGroup?[BusCount];

        private AudioSource? _music;
        private MusicDefinition? _currentTrack;
        private uint _pitchSeed = 0x9E3779B9u;

        /// <summary>Creates the service and its pool beneath the given transform.</summary>
        /// <param name="parent">Where the voice objects are hosted.</param>
        /// <param name="voices">How many sounds may overlap.</param>
        /// <param name="mixer">
        /// The project mixer. Optional: without one, bus levels are applied as multipliers on each
        /// source instead, which is correct but cannot express Master sitting above the rest.
        /// </param>
        public AudioService(Transform parent, int voices = 16, AudioMixer? mixer = null)
        {
            _mixer = mixer;
            ResolveGroups();

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
            source.outputAudioMixerGroup = _groups[(int)definition.Bus];
            source.volume = definition.Volume * SourceLevel(definition.Bus);
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
            _music.outputAudioMixerGroup = _groups[(int)AudioBus.Music];
            _music.volume = definition.Volume * SourceLevel(AudioBus.Music);
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

            if (_mixer != null)
            {
                // The graph takes it from here: a level set on Master reaches every sound beneath
                // it, including ones already playing, which a per-source multiplier cannot do.
                _mixer.SetFloat(ParameterFor(bus), Decibels(_busVolumes[(int)bus]));
                return;
            }

            if (bus is AudioBus.Music or AudioBus.Master && _music != null && _currentTrack != null)
            {
                _music.volume = _currentTrack.Volume * SourceLevel(AudioBus.Music);
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

        /// <summary>
        /// The exposed mixer parameter carrying a bus's level.
        /// </summary>
        /// <remarks>
        /// By convention rather than by reference, because a mixer's exposed parameters are strings
        /// and there is nothing to bind to. <see cref="ResolveGroups"/> reports any that are missing
        /// at start-up rather than leaving a slider that silently does nothing.
        /// </remarks>
        public static string ParameterFor(AudioBus bus) => bus switch
        {
            AudioBus.Master => "MasterVolume",
            AudioBus.Music => "MusicVolume",
            AudioBus.Sfx => "SfxVolume",
            AudioBus.Ui => "UiVolume",
            AudioBus.Voice => "VoiceVolume",
            AudioBus.Ambience => "AmbienceVolume",
            _ => "MasterVolume"
        };

        /// <summary>
        /// A normalised level as the mixer wants it.
        /// </summary>
        /// <remarks>
        /// Mixer levels are decibels, and loudness is logarithmic — halving a linear multiplier is
        /// not halving what a player hears. Silence is a floor rather than negative infinity because
        /// the mixer's own suspend threshold sits at −80.
        /// </remarks>
        public static float Decibels(float normalized01) =>
            normalized01 <= 0.0001f ? -80f : Mathf.Log10(normalized01) * 20f;

        /// <summary>Finds the group for each bus, and says so when one is missing.</summary>
        private void ResolveGroups()
        {
            if (_mixer == null)
            {
                return;
            }

            var groups = _mixer.FindMatchingGroups(string.Empty);

            for (var bus = 0; bus < BusCount; bus++)
            {
                var wanted = ((AudioBus)bus).ToString();

                for (var i = 0; i < groups.Length; i++)
                {
                    if (groups[i] != null && groups[i].name == wanted)
                    {
                        _groups[bus] = groups[i];
                        break;
                    }
                }

                if (_groups[bus] == null)
                {
                    Debug.LogWarning(
                        $"[Audio] The mixer has no '{wanted}' group; that bus falls back to Master.");
                }
                else if (!_mixer.GetFloat(ParameterFor((AudioBus)bus), out _))
                {
                    Debug.LogWarning(
                        $"[Audio] '{ParameterFor((AudioBus)bus)}' is not exposed on the mixer; the " +
                        $"{wanted} slider will do nothing.");
                }
            }
        }

        /// <summary>
        /// What a source must apply itself, on top of whatever the graph is already doing.
        /// </summary>
        /// <remarks>
        /// With a mixer that is nothing: the bus owns the level. Without one it is the bus level
        /// times Master's, so the root still reaches every sound — which the version before this
        /// got wrong, applying one bus per sound and letting Master touch only the music.
        /// </remarks>
        private float SourceLevel(AudioBus bus) =>
            _mixer != null
                ? 1f
                : CombinedLevel(bus, _busVolumes[(int)bus], _busVolumes[(int)AudioBus.Master]);

        /// <summary>
        /// What a sound on <paramref name="bus"/> plays at when there is no mixer graph to do it.
        /// </summary>
        /// <remarks>
        /// Master multiplies into every other bus, which is what the graph would have done for free.
        /// A sound already on Master is not multiplied by it twice.
        /// </remarks>
        public static float CombinedLevel(AudioBus bus, float busLevel, float masterLevel) =>
            bus == AudioBus.Master ? masterLevel : busLevel * masterLevel;

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
