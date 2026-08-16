using System.Text;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Items;
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
        private readonly int[] _lastTenths = new int[SkillLoadout.SlotCount];

        private int _lastHealth = -1;
        private int _lastEnemies = -1;
        private int _lastArena = -1;

        /// <summary>Which arena of the run this is. Zero hides it.</summary>
        public int ArenaNumber { get; set; }

        /// <summary>Refreshes the readout if anything it shows has changed.</summary>
        public void Render(GameSimulation simulation)
        {
            if (_output == null)
            {
                return;
            }

            EnsureSingleLine();

            var health = simulation.State.Player.Health.Current;
            var enemies = simulation.State.Enemies.AliveCount;

            // Rebuilding a string every frame would allocate for no reason; almost every frame
            // shows exactly what the last one did.
            if (health == _lastHealth && enemies == _lastEnemies && ArenaNumber == _lastArena &&
                SkillsUnchanged(simulation))
            {
                return;
            }

            _lastHealth = health;
            _lastEnemies = enemies;
            _lastArena = ArenaNumber;

            if (simulation.Phase == MatchPhase.Defeat)
            {
                _output.text = "DEFEATED";
                return;
            }

            _text.Clear();

            if (ArenaNumber > 0)
            {
                _text.Append("ARENA ").Append(ArenaNumber).Append("    ");
            }

            _text.Append("HP ").Append(health).Append("    ENEMIES ").Append(enemies);

            for (var index = 0; index < SkillLoadout.SlotCount; index++)
            {
                var slot = simulation.State.Player.Skills[index];

                if (!slot.IsEquipped)
                {
                    continue;
                }

                _lastCharges[index] = slot.Charges;
                _lastTenths[index] = Tenths(slot);

                _text.Append("    ").Append(Label(slot.Id)).Append(' ');

                if (slot.Charges > 0)
                {
                    _text.Append(slot.Charges);
                    continue;
                }

                // A spent skill shows when it returns, not merely that it is gone. A playtester
                // hoarded both skills for an entire run because nothing said they recharge — so
                // "how long" is the question worth answering, not "can I act".
                if (slot.CooldownRemaining > 0)
                {
                    var tenths = Tenths(slot);
                    _text.Append(tenths / 10).Append('.').Append(tenths % 10).Append('s');
                    continue;
                }

                _text.Append('-');
            }

            AppendBuild(simulation);

            _output.text = _text.ToString();
        }

        /// <summary>
        /// Writes out what the player is carrying.
        /// </summary>
        /// <remarks>
        /// On screen because the slice measures whether players can describe their build unprompted.
        /// A build they cannot see is one they cannot describe.
        /// </remarks>
        private void AppendBuild(GameSimulation simulation)
        {
            var items = simulation.State.Player.Items;

            for (var index = 0; index < items.Capacity; index++)
            {
                var id = items[index];

                if (id != ItemId.None)
                {
                    _text.Append("    ").Append(ItemCatalog.Name(id));
                }
            }
        }

        /// <summary>
        /// Forces the readout onto one line that may run past its box.
        /// </summary>
        /// <remarks>
        /// The line grew as skills and the build were added to it, and the authored box did not.
        /// With the default wrapping it folded onto a second line which the box then clipped, so on
        /// device the charges and the whole build were being drawn off-screen. Enforced here rather
        /// than in the scene so it holds however the text was authored.
        /// </remarks>
        private void EnsureSingleLine()
        {
            if (_output == null || _output.horizontalOverflow == HorizontalWrapMode.Overflow)
            {
                return;
            }

            _output.horizontalOverflow = HorizontalWrapMode.Overflow;
            _output.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private bool SkillsUnchanged(GameSimulation simulation)
        {
            for (var index = 0; index < SkillLoadout.SlotCount; index++)
            {
                var slot = simulation.State.Player.Skills[index];

                if (slot.Charges != _lastCharges[index] || Tenths(slot) != _lastTenths[index])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Recharge left, in tenths of a second.
        /// </summary>
        /// <remarks>
        /// Tenths rather than ticks, so the readout is rebuilt ten times a second while something
        /// is recharging instead of thirty. Nobody can read the difference.
        /// </remarks>
        private static int Tenths(in SkillSlot slot) =>
            slot.CooldownRemaining * 10 / SimulationConstants.TicksPerSecond;

        private static string Label(SkillId id) => id switch
        {
            SkillId.Dash => "DASH",
            SkillId.Skillshot => "SHOT",
            _ => "SKILL"
        };
    }
}
