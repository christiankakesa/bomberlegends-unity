using BomberLegends.Core;
using BomberLegends.Data.Audio;
using UnityEngine;

namespace BomberLegends.Services.Audio
{
    /// <summary>
    /// Plays sound and music through the project's mixer hierarchy.
    /// </summary>
    /// <remarks>
    /// Callers never touch <c>AudioSource</c> or <c>AudioMixer</c> directly. The implementation owns
    /// a pooled set of sources, enforces the per-effect concurrency limits declared on
    /// <see cref="SfxDefinition"/>, and converts linear volume sliders to decibels.
    /// </remarks>
    public interface IAudioService
    {
        /// <summary>
        /// Plays a one-shot effect. Requests beyond the effect's concurrency limit are dropped
        /// rather than queued, so a chain detonation cannot flood the mixer.
        /// </summary>
        /// <param name="definition">The effect to play.</param>
        /// <param name="worldPosition">
        /// Position for spatialised playback, or <see langword="null"/> to play without attenuation.
        /// </param>
        void PlaySfx(SfxDefinition definition, Vector3? worldPosition = null);

        /// <summary>
        /// Starts a music track, cross-fading from whatever is currently playing.
        /// Playing the track that is already active does nothing.
        /// </summary>
        void PlayMusic(MusicDefinition definition);

        /// <summary>Stops the current music track, fading out over <paramref name="fadeSeconds"/>.</summary>
        void StopMusic(float fadeSeconds = 1f);

        /// <summary>
        /// Sets the level of a mixer bus from a normalised slider value, where zero is silent and
        /// one is unattenuated. The implementation applies the logarithmic conversion.
        /// </summary>
        void SetBusVolume(AudioBus bus, float normalized01);

        /// <summary>Returns the current normalised level of a bus.</summary>
        float GetBusVolume(AudioBus bus);

        /// <summary>
        /// Silences everything immediately. Used when the application is backgrounded and when a
        /// match is torn down.
        /// </summary>
        void StopAll();
    }
}
