using System;
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
    /// It also carries the one performance number a phone can report about itself: frame time, as
    /// the median and the 99th percentile over the last ten seconds. Android's own frame counters
    /// describe its view system and not Unity's surface, so without this line there is no honest
    /// frame-time figure to be had on a device without a cable and the profiler.
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

        [SerializeField]
        [Tooltip("Show frame time — median and 99th percentile over the last ten seconds.")]
        private bool _showFrameTime = true;

        /// <summary>Ten seconds at sixty frames a second.</summary>
        private const int FrameWindow = 600;

        private readonly StringBuilder _builder = new StringBuilder(1024);
        private readonly System.Collections.Generic.Queue<string> _entries =
            new System.Collections.Generic.Queue<string>();
        private readonly float[] _frames = new float[FrameWindow];
        private readonly float[] _sorted = new float[FrameWindow];
        private int _frameCount;
        private int _nextFrame;
        private float _nextReport;
        private string _frameLine = string.Empty;

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

        private void Update()
        {
            if (!_showFrameTime)
            {
                return;
            }

            _frames[_nextFrame] = Time.unscaledDeltaTime * 1000f;
            _nextFrame = (_nextFrame + 1) % FrameWindow;
            if (_frameCount < FrameWindow)
            {
                _frameCount++;
            }

            // Reported once a second. Sorting six hundred floats and building one string at that
            // rate is nothing; doing it every frame would be measuring the overlay.
            if (Time.unscaledTime < _nextReport)
            {
                return;
            }

            _nextReport = Time.unscaledTime + 1f;

            Array.Copy(_frames, _sorted, _frameCount);
            Array.Sort(_sorted, 0, _frameCount);

            _frameLine =
                $"FRAME  p50 {Percentile(_sorted, _frameCount, 0.5f):F1} ms  ·  " +
                $"p99 {Percentile(_sorted, _frameCount, 0.99f):F1} ms  ·  {_frameCount / 60}s";

            Redraw();
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

            Redraw();
        }

        private void Redraw()
        {
            _builder.Clear();

            if (_frameLine.Length > 0)
            {
                _builder.AppendLine(_frameLine);
            }

            foreach (var entry in _entries)
            {
                _builder.AppendLine(entry);
            }

            if (_output != null)
            {
                _output.text = _builder.ToString();
            }
        }

        /// <summary>
        /// The value below which the given share of a sorted sample falls.
        /// </summary>
        /// <remarks>
        /// Nearest rank, which is what a frame-time percentile wants: the 99th of six hundred frames
        /// is the sixth-worst frame, not an interpolation between two of them.
        /// </remarks>
        public static float Percentile(float[] sorted, int count, float share)
        {
            if (count <= 0)
            {
                return 0f;
            }

            var rank = Mathf.CeilToInt(share * count) - 1;

            return sorted[Mathf.Clamp(rank, 0, count - 1)];
        }
    }
}
