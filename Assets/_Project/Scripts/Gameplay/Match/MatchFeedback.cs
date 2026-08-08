using BomberLegends.Data.Audio;
using BomberLegends.Gameplay.Board;
using BomberLegends.Gameplay.Camera;
using BomberLegends.Services.Audio;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Events;
using UnityEngine;

namespace BomberLegends.Gameplay.Match
{
    /// <summary>
    /// Turns the simulation's event stream into sound and camera movement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately free of any per-event branching. It walks the events, looks each one up in a
    /// <see cref="FeedbackTable"/> and applies whatever it finds, so a new moment gets feedback by
    /// gaining a row in an asset rather than a case in a system.
    /// </para>
    /// <para>
    /// Separate from <see cref="MatchViewSynchroniser"/> on purpose. That class owns pooled objects
    /// with lifetimes; this one owns nothing and only reacts, and mixing the two is how the switch
    /// statement this replaces came to exist.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MatchFeedback : MonoBehaviour
    {
        private IAudioService? _audio;
        private FeedbackTable? _table;
        private BoardProjector? _projector;
        private MatchCameraRig? _camera;

        /// <summary>Wires the feedback layer to everything it reads and drives.</summary>
        public void Begin(
            IAudioService audio,
            FeedbackTable table,
            BoardProjector projector,
            MatchCameraRig? camera)
        {
            _audio = audio;
            _table = table;
            _projector = projector;
            _camera = camera;
        }

        /// <summary>Reacts to everything the simulation announced this tick.</summary>
        public void Consume(GameSimulation simulation)
        {
            if (_audio == null || _table == null || _projector == null)
            {
                return;
            }

            var events = simulation.Events;

            for (var i = 0; i < events.Count; i++)
            {
                var simEvent = events[i];

                if (!ShouldReact(simEvent) || !_table.TryGet(simEvent.Type, out var entry))
                {
                    continue;
                }

                if (entry.Sfx != null)
                {
                    _audio.PlaySfx(entry.Sfx, _projector.TileToWorld(simEvent.Coord));
                }

                if (entry.ShakeStrength > 0f && _camera != null)
                {
                    _camera.Shake(entry.ShakeStrength, entry.ShakeSeconds);
                }
            }
        }

        /// <summary>
        /// The one place an event's fields are inspected rather than just its type.
        /// </summary>
        /// <remarks>
        /// Damage is reported for enemies as well as the player, and the player being hurt is the
        /// single most important sound in the game — it cannot share a cue with chipping a mob.
        /// Enemies announce themselves through <see cref="SimEventType.EnemyKilled"/> instead.
        /// </remarks>
        private static bool ShouldReact(in SimEvent simEvent) =>
            simEvent.Type != SimEventType.DamageTaken || simEvent.EntityId == 0;
    }
}
