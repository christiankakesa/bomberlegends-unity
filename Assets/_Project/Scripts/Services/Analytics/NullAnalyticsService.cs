using UnityEngine;

namespace BomberLegends.Services.Analytics
{
    /// <summary>
    /// The analytics implementation used until a provider is integrated in Milestone 9.
    /// </summary>
    /// <remarks>
    /// Discards every event in a build. In the Editor it logs them instead, which is how event names
    /// and payloads get verified while the feature that raises them is being written, rather than
    /// months later against a live dashboard.
    /// </remarks>
    public sealed class NullAnalyticsService : IAnalyticsService
    {
        private readonly bool _logInEditor;

        /// <summary>Creates the service.</summary>
        /// <param name="logInEditor">Whether to log events to the console in the Editor.</param>
        public NullAnalyticsService(bool logInEditor = true)
        {
            _logInEditor = logInEditor;
        }

        /// <inheritdoc />
        public void Track(string eventName, in AnalyticsPayload payload)
        {
            if (!Application.isEditor || !_logInEditor)
            {
                return;
            }

            LogEvent(eventName, payload);
        }

        private static void LogEvent(string eventName, in AnalyticsPayload payload)
        {
            var builder = new System.Text.StringBuilder(96);
            builder.Append("[analytics] ").Append(eventName);

            for (var i = 0; i < payload.Count; i++)
            {
                var field = payload[i];
                builder.Append(i == 0 ? " { " : ", ").Append(field.Name).Append('=').Append(field.Value);
            }

            if (payload.Count > 0)
            {
                builder.Append(" }");
            }

            Debug.Log(builder.ToString());
        }
    }
}
