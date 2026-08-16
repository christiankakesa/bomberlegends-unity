using System.Collections.Generic;
using BomberLegends.Core;
using BomberLegends.Simulation.Events;
using UnityEngine;

namespace BomberLegends.Data.Audio
{
    /// <summary>
    /// Builds a working feedback table from generated sounds, for when none has been authored.
    /// </summary>
    /// <remarks>
    /// So the game is never silent by default. A designer replacing this authors a
    /// <see cref="FeedbackTable"/> asset and assigns it; nothing in code has to change, which is
    /// the point of the table existing at all.
    /// </remarks>
    public static class PlaceholderFeedback
    {
        /// <summary>Creates a table covering every moment that currently matters.</summary>
        public static FeedbackTable CreateTable()
        {
            var table = ScriptableObject.CreateInstance<FeedbackTable>();
            table.hideFlags = HideFlags.HideAndDontSave;

            var entries = new List<FeedbackEntry>
            {
                Entry(SimEventType.BombPlaced, Sfx(ProceduralClips.Thump(), volume: 0.75f)),

                // Bound to the detonation, never to the blast tiles. One chain lights a hundred
                // tiles on a single tick, and a sound per tile is noise, not an explosion.
                Entry(SimEventType.BombDetonated, Sfx(ProceduralClips.Boom(), volume: 0.95f, maxConcurrent: 3),
                    shake: 0.5f, seconds: 0.28f),

                Entry(SimEventType.BlockDestroyed,
                    Sfx(ProceduralClips.Crunch(), volume: 0.65f, maxConcurrent: 3, retrigger: 0.05f),
                    shake: 0.08f, seconds: 0.10f),

                Entry(SimEventType.EnemyKilled, Sfx(ProceduralClips.Pop(), volume: 0.8f),
                    shake: 0.12f, seconds: 0.12f),

                // The two that must never be missed. Loud, unique, and the only ones that shake
                // hard enough to interrupt what the player was doing.
                Entry(SimEventType.DamageTaken, Sfx(ProceduralClips.Hurt(), volume: 1f, pitchVariation: 0.02f),
                    shake: 0.85f, seconds: 0.35f),

                Entry(SimEventType.PlayerDied, Sfx(ProceduralClips.Death(), volume: 0.95f, pitchVariation: 0f),
                    shake: 1f, seconds: 0.7f),

                Entry(SimEventType.DashStarted, Sfx(ProceduralClips.Whoosh(), volume: 0.7f)),
                Entry(SimEventType.ProjectileFired, Sfx(ProceduralClips.Shot(), volume: 0.65f)),
                Entry(SimEventType.ItemAcquired, Sfx(ProceduralClips.Pickup())),
                Entry(SimEventType.ArenaCleared, Sfx(ProceduralClips.Fanfare()))
            };

            table.SetEntries(entries.ToArray());
            return table;
        }

        private static FeedbackEntry Entry(
            SimEventType type, SfxDefinition sfx, float shake = 0f, float seconds = 0f) =>
            new FeedbackEntry
            {
                Event = type,
                Sfx = sfx,
                ShakeStrength = shake,
                ShakeSeconds = seconds
            };

        /// <summary>Wraps a generated clip in the same definition an authored one would use.</summary>
        private static SfxDefinition Sfx(
            AudioClip clip,
            float volume = 0.7f,
            float pitchVariation = 0.06f,
            int maxConcurrent = 4,
            float retrigger = 0.03f)
        {
            var definition = ScriptableObject.CreateInstance<SfxDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.Configure(
                new[] { clip }, AudioBus.Sfx, volume, pitchVariation, maxConcurrent, retrigger);

            return definition;
        }
    }
}
