using System;
using BomberLegends.Core;
using UnityEngine;

namespace BomberLegends.Services.Settings
{
    /// <summary>
    /// Player-facing options, as stored and restored across sessions.
    /// </summary>
    /// <remarks>
    /// Volumes are normalised slider values, not decibels: the conversion to the mixer's
    /// logarithmic scale belongs to the audio service. Accessibility toggles are first-class rather
    /// than an afterthought, because screen shake and full-screen flashes are exactly the effects
    /// that make a bloom-heavy game unplayable for some players.
    /// </remarks>
    [Serializable]
    public struct SettingsData
    {
        [SerializeField, Range(0f, 1f)] private float _masterVolume;
        [SerializeField, Range(0f, 1f)] private float _musicVolume;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume;
        [SerializeField, Range(0f, 1f)] private float _uiVolume;
        [SerializeField] private int _qualityTier;
        [SerializeField] private bool _screenShakeEnabled;
        [SerializeField] private bool _screenFlashEnabled;
        [SerializeField] private bool _hapticsEnabled;

        /// <summary>Creates a settings snapshot.</summary>
        public SettingsData(
            float masterVolume,
            float musicVolume,
            float sfxVolume,
            float uiVolume,
            int qualityTier,
            bool screenShakeEnabled,
            bool screenFlashEnabled,
            bool hapticsEnabled)
        {
            _masterVolume = masterVolume;
            _musicVolume = musicVolume;
            _sfxVolume = sfxVolume;
            _uiVolume = uiVolume;
            _qualityTier = qualityTier;
            _screenShakeEnabled = screenShakeEnabled;
            _screenFlashEnabled = screenFlashEnabled;
            _hapticsEnabled = hapticsEnabled;
        }

        /// <summary>The values a player starts with before touching the options screen.</summary>
        public static SettingsData Default =>
            new SettingsData(1f, 0.7f, 1f, 0.8f, qualityTier: 1, true, true, true);

        /// <summary>Normalised level of the master bus.</summary>
        public float MasterVolume => _masterVolume;

        /// <summary>Normalised level of the music bus.</summary>
        public float MusicVolume => _musicVolume;

        /// <summary>Normalised level of the sound effects bus.</summary>
        public float SfxVolume => _sfxVolume;

        /// <summary>Normalised level of the interface bus.</summary>
        public float UiVolume => _uiVolume;

        /// <summary>Selected quality tier index.</summary>
        public int QualityTier => _qualityTier;

        /// <summary>Whether camera shake is applied on explosions.</summary>
        public bool ScreenShakeEnabled => _screenShakeEnabled;

        /// <summary>Whether full-screen flashes are applied on explosions.</summary>
        public bool ScreenFlashEnabled => _screenFlashEnabled;

        /// <summary>Whether haptic feedback fires on impacts.</summary>
        public bool HapticsEnabled => _hapticsEnabled;

        /// <summary>Returns the normalised level for a bus, or one for buses without a slider.</summary>
        public float GetBusVolume(AudioBus bus) => bus switch
        {
            AudioBus.Master => _masterVolume,
            AudioBus.Music => _musicVolume,
            AudioBus.Sfx => _sfxVolume,
            AudioBus.Ui => _uiVolume,
            _ => 1f
        };
    }

    /// <summary>
    /// Reads and applies player options.
    /// </summary>
    /// <remarks>
    /// Applying settings takes effect immediately and persists through the save service. Listeners
    /// react to <see cref="Changed"/> rather than polling, so the audio service and the options
    /// screen never need to know about each other.
    /// </remarks>
    public interface ISettingsService
    {
        /// <summary>The settings currently in effect.</summary>
        SettingsData Current { get; }

        /// <summary>Raised after new settings have been applied.</summary>
        event Action<SettingsData>? Changed;

        /// <summary>Applies and persists a new set of options.</summary>
        void Apply(in SettingsData settings);

        /// <summary>Restores the defaults and persists them.</summary>
        void ResetToDefaults();
    }
}
