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
        PlayerBlocked = 3,

        /// <summary>A bomb was placed. <c>EntityId</c> is its slot, <c>Value</c> its blast range.</summary>
        BombPlaced = 4,

        /// <summary>A bomb went off. <c>EntityId</c> is its slot, <c>Value</c> its blast range.</summary>
        BombDetonated = 5,

        /// <summary>A tile caught fire. Raised once per tile, however many arms reach it.</summary>
        BlastSpawned = 6,

        /// <summary>A destructible block was destroyed.</summary>
        BlockDestroyed = 7,

        /// <summary>
        /// Something took damage. <c>EntityId</c> is zero for the player or the enemy slot plus one;
        /// <c>Value</c> is the amount.
        /// </summary>
        DamageTaken = 8,

        /// <summary>An enemy ran out of health. <c>EntityId</c> is its slot plus one.</summary>
        EnemyKilled = 9,

        /// <summary>The player ran out of health.</summary>
        PlayerDied = 10,

        /// <summary>
        /// A skill was used. <c>EntityId</c> is its loadout slot, <c>Value</c> its
        /// <see cref="Skills.SkillId"/>.
        /// </summary>
        SkillUsed = 11,

        /// <summary>A dash began. <c>Value</c> is how many ticks it lasts.</summary>
        DashStarted = 12,

        /// <summary>A skillshot was fired. <c>EntityId</c> is its slot in the projectile buffer.</summary>
        ProjectileFired = 13,

        /// <summary>
        /// A skillshot stopped. <c>EntityId</c> is its projectile slot; <c>Value</c> is the damage
        /// dealt, or zero when it hit terrain or ran out of range.
        /// </summary>
        ProjectileEnded = 14,

        /// <summary>
        /// An item was taken. <c>EntityId</c> is its inventory slot, <c>Value</c> its
        /// <see cref="Items.ItemId"/>.
        /// </summary>
        ItemAcquired = 15
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
