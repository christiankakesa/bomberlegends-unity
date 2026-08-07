using BomberLegends.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Match
{
    /// <summary>
    /// A bare readout of health and enemies remaining.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately minimal: enough to evaluate whether the damage rules feel right, and nothing
    /// more. The real interface arrives once the loadout exists and there is something worth
    /// designing around.
    /// </para>
    /// <para>
    /// Lives in Gameplay rather than the UI assembly because it reads the live simulation directly,
    /// and UI may not reference Gameplay. When this becomes a real HUD it should move behind an event
    /// channel in Data, which is the seam that lets UI react without seeing gameplay at all.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MatchHudView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Where the readout is written.")]
        private Text? _output;

        private int _lastHealth = -1;
        private int _lastEnemies = -1;

        /// <summary>Refreshes the readout if anything it shows has changed.</summary>
        public void Render(GameSimulation simulation)
        {
            if (_output == null)
            {
                return;
            }

            var health = simulation.State.Player.Health.Current;
            var enemies = simulation.State.Enemies.AliveCount;

            // Rebuilding a string every frame would allocate for no reason; almost every frame
            // shows exactly what the last one did.
            if (health == _lastHealth && enemies == _lastEnemies)
            {
                return;
            }

            _lastHealth = health;
            _lastEnemies = enemies;

            _output.text = simulation.Phase == MatchPhase.Defeat
                ? "DEFEATED"
                : $"HP {health}    ENEMIES {enemies}";
        }
    }
}
