using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Events
{
    /// <summary>Something the simulation did that the view or audio may want to react to.</summary>
    /// <remarks>
    /// Continuous state such as the player's position is read from the simulation state, not from
    /// events. Events are for discrete moments — the things that spawn an effect or a sound exactly
    /// once. More arrive with bombs and blasts in Milestone 2.
    /// </remarks>
    public enum SimEventType : byte
    {
        /// <summary>Reserved so a zeroed event is obviously invalid.</summary>
        None = 0,

        /// <summary>The player was placed at their spawn tile.</summary>
        PlayerSpawned = 1,

        /// <summary>The player's occupied tile changed.</summary>
        PlayerTileEntered = 2,

        /// <summary>The player pressed into something solid and stopped.</summary>
        PlayerBlocked = 3
    }

    /// <summary>
    /// One simulation event, as a value type.
    /// </summary>
    /// <remarks>
    /// A flat struct rather than a class hierarchy so the whole per-tick event stream lives in a
    /// preallocated buffer and costs no allocation. Fields are generic on purpose; their meaning
    /// depends on <see cref="Type"/> and is documented on each event kind.
    /// </remarks>
    public readonly struct SimEvent : IEquatable<SimEvent>
    {
        /// <summary>What happened.</summary>
        public readonly SimEventType Type;

        /// <summary>The tile the event happened on.</summary>
        public readonly GridCoord Coord;

        /// <summary>Which entity it happened to, or zero for the player.</summary>
        public readonly int EntityId;

        /// <summary>Event-specific magnitude, such as a direction or an amount.</summary>
        public readonly int Value;

        /// <summary>Creates an event.</summary>
        public SimEvent(SimEventType type, GridCoord coord, int entityId = 0, int value = 0)
        {
            Type = type;
            Coord = coord;
            EntityId = entityId;
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(SimEvent other) =>
            Type == other.Type && Coord.Equals(other.Coord) &&
            EntityId == other.EntityId && Value == other.Value;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SimEvent other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Type;
                hash = (hash * 397) ^ Coord.GetHashCode();
                hash = (hash * 397) ^ EntityId;
                return (hash * 397) ^ Value;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{Type} at {Coord} (entity {EntityId}, value {Value})";
    }
}
