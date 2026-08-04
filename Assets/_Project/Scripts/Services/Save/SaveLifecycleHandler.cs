using UnityEngine;

namespace BomberLegends.Services.Save
{
    /// <summary>
    /// Bridges Unity's application lifecycle to the save service.
    /// </summary>
    /// <remarks>
    /// Lives on the persistent bootstrap scene for the lifetime of the process. Its whole purpose is
    /// to make sure progress reaches storage before the process stops existing: on Android,
    /// <see cref="OnApplicationPause"/> is frequently the last callback delivered before the app is
    /// killed, and a save that has not been flushed by then is simply gone. That is the single most
    /// common cause of "the game lost my progress" reviews, so these writes are synchronous.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SaveLifecycleHandler : MonoBehaviour
    {
        [SerializeField, Min(1f)]
        [Tooltip("Seconds between background flushes while there are unsaved changes.")]
        private float _autoSaveIntervalSeconds = 30f;

        private ISaveService? _saveService;
        private float _nextAutoSaveTime;

        /// <summary>Supplies the service to flush. Called by the bootstrap composition root.</summary>
        public void Initialise(ISaveService saveService)
        {
            _saveService = saveService;
            _nextAutoSaveTime = Time.unscaledTime + _autoSaveIntervalSeconds;
        }

        private void Update()
        {
            if (_saveService == null || !_saveService.IsDirty || Time.unscaledTime < _nextAutoSaveTime)
            {
                return;
            }

            _nextAutoSaveTime = Time.unscaledTime + _autoSaveIntervalSeconds;
            _saveService.FlushImmediate();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                FlushIfNeeded();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FlushIfNeeded();
            }
        }

        private void OnApplicationQuit() => FlushIfNeeded();

        private void FlushIfNeeded()
        {
            if (_saveService is { IsDirty: true })
            {
                _saveService.FlushImmediate();
            }
        }
    }
}
