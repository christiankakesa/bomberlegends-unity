using System;

namespace BomberLegends.Services.Analytics
{
    /// <summary>A single named value attached to an analytics event.</summary>
    public readonly struct AnalyticsField
    {
        /// <summary>The field name as it appears in the event schema.</summary>
        public readonly string Name;

        /// <summary>The value. Booleans are recorded as zero or one.</summary>
        public readonly long Value;

        /// <summary>Creates a field.</summary>
        public AnalyticsField(string name, long value)
        {
            Name = name;
            Value = value;
        }
    }

    /// <summary>
    /// The values attached to an analytics event.
    /// </summary>
    /// <remarks>
    /// A fixed number of inline fields rather than a dictionary, so recording an event allocates
    /// nothing. Six covers the largest event in the current schema (<c>match_ended</c>). Values are
    /// numeric on purpose: counts and durations are what get analysed, and free-form strings turn
    /// into an unqueryable mess.
    /// </remarks>
    public readonly struct AnalyticsPayload
    {
        /// <summary>The maximum number of fields a single event can carry.</summary>
        public const int MaxFields = 6;

        private readonly AnalyticsField _field0;
        private readonly AnalyticsField _field1;
        private readonly AnalyticsField _field2;
        private readonly AnalyticsField _field3;
        private readonly AnalyticsField _field4;
        private readonly AnalyticsField _field5;

        /// <summary>The number of fields set.</summary>
        public readonly int Count;

        private AnalyticsPayload(
            int count,
            AnalyticsField field0,
            AnalyticsField field1,
            AnalyticsField field2,
            AnalyticsField field3,
            AnalyticsField field4,
            AnalyticsField field5)
        {
            Count = count;
            _field0 = field0;
            _field1 = field1;
            _field2 = field2;
            _field3 = field3;
            _field4 = field4;
            _field5 = field5;
        }

        /// <summary>An event with no attached values.</summary>
        public static AnalyticsPayload Empty => default;

        /// <summary>
        /// Returns a copy of this payload with an additional field.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="name"/> is null, empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">The payload already holds <see cref="MaxFields"/> fields.</exception>
        public AnalyticsPayload With(string name, long value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Analytics field name must not be empty.", nameof(name));
            }

            if (Count >= MaxFields)
            {
                throw new InvalidOperationException(
                    $"An analytics payload carries at most {MaxFields} fields. Split the event instead.");
            }

            var field = new AnalyticsField(name, value);
            return Count switch
            {
                0 => new AnalyticsPayload(1, field, _field1, _field2, _field3, _field4, _field5),
                1 => new AnalyticsPayload(2, _field0, field, _field2, _field3, _field4, _field5),
                2 => new AnalyticsPayload(3, _field0, _field1, field, _field3, _field4, _field5),
                3 => new AnalyticsPayload(4, _field0, _field1, _field2, field, _field4, _field5),
                4 => new AnalyticsPayload(5, _field0, _field1, _field2, _field3, field, _field5),
                _ => new AnalyticsPayload(6, _field0, _field1, _field2, _field3, _field4, field)
            };
        }

        /// <summary>Returns a copy of this payload with an additional boolean field, recorded as zero or one.</summary>
        public AnalyticsPayload With(string name, bool value) => With(name, value ? 1L : 0L);

        /// <summary>Returns the field at <paramref name="index"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is outside <see cref="Count"/>.</exception>
        public AnalyticsField this[int index] => index switch
        {
            0 when Count > 0 => _field0,
            1 when Count > 1 => _field1,
            2 when Count > 2 => _field2,
            3 when Count > 3 => _field3,
            4 when Count > 4 => _field4,
            5 when Count > 5 => _field5,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, "No field at that index.")
        };
    }

    /// <summary>
    /// Records gameplay telemetry.
    /// </summary>
    /// <remarks>
    /// This interface exists from the first milestone specifically so instrumentation can be written
    /// alongside the features it measures. Until a provider is wired up in Milestone 9 the
    /// implementation is <see cref="NullAnalyticsService"/>, which means call sites cost nothing and
    /// no feature has to be revisited to add tracking later.
    /// </remarks>
    public interface IAnalyticsService
    {
        /// <summary>Records an event.</summary>
        /// <param name="eventName">Snake-case event name, for example <c>match_ended</c>.</param>
        /// <param name="payload">Values attached to the event.</param>
        void Track(string eventName, in AnalyticsPayload payload);
    }
}
