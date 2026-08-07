using System.Text;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Skills;
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

        private readonly StringBuilder _text = new StringBuilder(96);
        private readonly int[] _lastCharges = new int[SkillLoadout.SlotCount];

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
            if (health == _lastHealth && enemies == _lastEnemies && ChargesUnchanged(simulation))
            {
                return;
            }

            _lastHealth = health;
            _lastEnemies = enemies;

            if (simulation.Phase == MatchPhase.Defeat)
            {
                _output.text = "DEFEATED";
                return;
            }

            _text.Clear();
            _text.Append("HP ").Append(health).Append("    ENEMIES ").Append(enemies);

            for (var index = 0; index < SkillLoadout.SlotCount; index++)
            {
                var slot = simulation.State.Player.Skills[index];

                if (!slot.IsEquipped)
                {
                    continue;
                }

                _lastCharges[index] = slot.Charges;

                _text.Append("    ").Append(Label(slot.Id)).Append(' ');

                // Charges rather than a timer: what a player needs mid-fight is whether they can
                // act, not how long until they can.
                if (slot.Charges > 0)
                {
                    _text.Append(slot.Charges);
                }
                else
                {
                    _text.Append('-');
                }
            }

            _output.text = _text.ToString();
        }

        private bool ChargesUnchanged(GameSimulation simulation)
        {
            for (var index = 0; index < SkillLoadout.SlotCount; index++)
            {
                if (simulation.State.Player.Skills[index].Charges != _lastCharges[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static string Label(SkillId id) => id switch
        {
            SkillId.Dash => "DASH",
            SkillId.Skillshot => "SHOT",
            _ => "SKILL"
        };
    }
}
