using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Services.Diagnostics
{
    /// <summary>
    /// Shows errors and exceptions on screen in development builds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A phone gives no console. Without this, a failure that only happens on device is diagnosed by
    /// guesswork or by wiring up a cable, and the difference between the Editor and a player build —
    /// stripping, IL2CPP, platform APIs — is exactly where those failures live.
    /// </para>
    /// <para>
    /// Disables itself entirely outside development builds, so it can never reach a player.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class DeviceLogOverlay : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Where messages are written. Left empty, the overlay does nothing.")]
        private Text? _output;

        [SerializeField, Min(1)]
        [Tooltip("How many recent messages to keep on screen.")]
        private int _maxEntries = 12;

        private readonly StringBuilder _builder = new StringBuilder(1024);
        private readonly System.Collections.Generic.Queue<string> _entries =
            new System.Collections.Generic.Queue<string>();

        private void Awake()
        {
            if (!Debug.isDebugBuild || _output == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _output.text = string.Empty;
            Application.logMessageReceived += OnLogMessage;
        }

        private void OnDestroy()
        {
            if (Debug.isDebugBuild)
            {
                Application.logMessageReceived -= OnLogMessage;
            }
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            _entries.Enqueue(condition);
            while (_entries.Count > _maxEntries)
            {
                _entries.Dequeue();
            }

            _builder.Clear();
            foreach (var entry in _entries)
            {
                _builder.AppendLine(entry);
            }

            if (_output != null)
            {
                _output.text = _builder.ToString();
            }
        }
    }
}
