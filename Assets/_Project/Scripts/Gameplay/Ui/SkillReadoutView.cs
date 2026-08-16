using BomberLegends.Input;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Skills;
using UnityEngine;

namespace BomberLegends.Gameplay.Ui
{
    /// <summary>
    /// Keeps the on-screen skill buttons showing what the simulation actually knows.
    /// </summary>
    /// <remarks>
    /// Reads charges and recharge straight from the loadout each frame. The values already exist on
    /// <see cref="SkillSlot"/> — nothing here is new state, it was simply never shown.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SkillReadoutView : MonoBehaviour
    {
        private SkillTouchButton[] _buttons = System.Array.Empty<SkillTouchButton>();

        /// <summary>Binds the readout to the buttons it drives.</summary>
        public void Begin(SkillTouchButton[]? buttons) =>
            _buttons = buttons ?? System.Array.Empty<SkillTouchButton>();

        /// <summary>Pushes the current state of every slot onto its button.</summary>
        public void Render(GameSimulation simulation)
        {
            for (var index = 0; index < _buttons.Length && index < SkillLoadout.SlotCount; index++)
            {
                var button = _buttons[index];

                if (button == null)
                {
                    continue;
                }

                var slot = simulation.State.Player.Skills[index];

                button.SetReadiness(slot.IsReady, slot.RechargePercent / 100f);
            }
        }
    }
}
