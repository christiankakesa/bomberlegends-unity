using BomberLegends.Core;
using BomberLegends.Data.Audio;
using UnityEngine;

namespace BomberLegends.Services.Audio
{
    /// <summary>
    /// The audio service used before the mixer and source pool exist.
    /// </summary>
    /// <remarks>
    /// Records bus levels so settings round-trip correctly, and plays nothing. Silence is the
    /// natural degraded state for audio, so this fails quietly rather than throwing — unlike asset
    /// loading, a missing sound does not leave the game in a broken state. It warns once in the
    /// Editor so the absence is never mistaken for a bug in the mixer.
    /// </remarks>
    public sealed class SilentAudioService : IAudioService
    {
        private readonly float[] _busVolumes = new float[6];
        private bool _hasWarned;

        /// <summary>Creates the service with every bus unattenuated.</summary>
        public SilentAudioService()
        {
            for (var i = 0; i < _busVolumes.Length; i++)
            {
                _busVolumes[i] = 1f;
            }
        }

        /// <inheritdoc />
        public void PlaySfx(SfxDefinition definition, Vector3? worldPosition = null) => WarnOnce();

        /// <inheritdoc />
        public void PlayMusic(MusicDefinition definition) => WarnOnce();

        /// <inheritdoc />
        public void StopMusic(float fadeSeconds = 1f)
        {
        }

        /// <inheritdoc />
        public void SetBusVolume(AudioBus bus, float normalized01) =>
            _busVolumes[(int)bus] = Mathf.Clamp01(normalized01);

        /// <inheritdoc />
        public float GetBusVolume(AudioBus bus) => _busVolumes[(int)bus];

        /// <inheritdoc />
        public void StopAll()
        {
        }

        private void WarnOnce()
        {
            if (_hasWarned || !Application.isEditor)
            {
                return;
            }

            _hasWarned = true;
            Debug.LogWarning(
                "[Audio] Playback was requested but no mixer is wired up yet, so nothing is audible.");
        }
    }
}
