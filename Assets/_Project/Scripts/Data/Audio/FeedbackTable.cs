using System;
using System.Collections.Generic;
using BomberLegends.Simulation.Events;
using UnityEngine;

namespace BomberLegends.Data.Audio
{
    /// <summary>What a moment in the simulation should look and sound like.</summary>
    [Serializable]
    public struct FeedbackEntry
    {
        [Tooltip("The simulation event this reacts to.")]
        public SimEventType Event;

        [Tooltip("Sound to play. Optional.")]
        public SfxDefinition? Sfx;

        [Range(0f, 1f)]
        [Tooltip("How hard the camera is knocked. Zero for no shake.")]
        public float ShakeStrength;

        [Range(0f, 1f)]
        [Tooltip("How long the knock takes to settle, in seconds.")]
        public float ShakeSeconds;
    }

    /// <summary>
    /// Binds simulation events to the feedback they produce.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole point is that nothing in code names a particular sound. The view walks the event
    /// stream and looks each event up here, so binding audio to a new moment is a row in an asset
    /// rather than an edit to a system — and effects can be retuned while the game is running,
    /// which is the only practical way to tune feel.
    /// </para>
    /// <para>
    /// The alternative is a <c>case</c> per event inside the view, which is how it worked for the
    /// three effects that existed before this. That does not survive a dozen events, and it quietly
    /// makes every feel change an engineering ticket.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Bomber Legends/Audio/Feedback Table", fileName = "FeedbackTable")]
    public sealed class FeedbackTable : ScriptableObject
    {
        [SerializeField]
        [Tooltip("One row per moment worth reacting to. Events with no row produce nothing.")]
        private FeedbackEntry[] _entries = Array.Empty<FeedbackEntry>();

        private Dictionary<SimEventType, FeedbackEntry>? _lookup;

        /// <summary>Every binding, in authoring order.</summary>
        public IReadOnlyList<FeedbackEntry> Entries => _entries;

        /// <summary>Replaces every binding. Used to build a table in code when none was authored.</summary>
        public void SetEntries(FeedbackEntry[] entries)
        {
            _entries = entries ?? Array.Empty<FeedbackEntry>();
            _lookup = null;
        }

        /// <summary>Finds the feedback bound to an event, if any.</summary>
        public bool TryGet(SimEventType type, out FeedbackEntry entry)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<SimEventType, FeedbackEntry>(_entries.Length);

                for (var i = 0; i < _entries.Length; i++)
                {
                    // Last row wins, so an override can simply be appended rather than hunted down.
                    _lookup[_entries[i].Event] = _entries[i];
                }
            }

            return _lookup.TryGetValue(type, out entry);
        }
    }
}
