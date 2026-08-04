using System;
using BomberLegends.Core;
using BomberLegends.Services.Audio;
using BomberLegends.Services.Save;

namespace BomberLegends.Services.Settings
{
    /// <summary>
    /// Keeps player options in the save and pushes them to the systems that act on them.
    /// </summary>
    /// <remarks>
    /// Options live inside the save payload rather than in a store of their own, so they migrate,
    /// back up and eventually sync to a server through exactly the same path as progress.
    /// </remarks>
    public sealed class SettingsService : ISettingsService
    {
        private readonly ISaveService _save;
        private readonly IAudioService _audio;

        /// <summary>Creates the service.</summary>
        public SettingsService(ISaveService save, IAudioService audio)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        /// <inheritdoc />
        public SettingsData Current => _save.Data.Settings;

        /// <inheritdoc />
        public event Action<SettingsData>? Changed;

        /// <inheritdoc />
        public void Apply(in SettingsData settings)
        {
            _save.Data.Settings = settings;
            _save.MarkDirty();
            PushToAudio(settings);
            Changed?.Invoke(settings);
        }

        /// <inheritdoc />
        public void ResetToDefaults() => Apply(SettingsData.Default);

        /// <summary>
        /// Pushes the loaded settings to their systems without marking the save dirty. Called once
        /// at start-up, after the save has been read.
        /// </summary>
        public void ApplyLoaded()
        {
            var settings = Current;
            PushToAudio(settings);
            Changed?.Invoke(settings);
        }

        private void PushToAudio(in SettingsData settings)
        {
            _audio.SetBusVolume(AudioBus.Master, settings.MasterVolume);
            _audio.SetBusVolume(AudioBus.Music, settings.MusicVolume);
            _audio.SetBusVolume(AudioBus.Sfx, settings.SfxVolume);
            _audio.SetBusVolume(AudioBus.Ui, settings.UiVolume);
        }
    }
}
