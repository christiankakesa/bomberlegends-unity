using System;
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

        /// <summary>Where the readout leaves the counters and starts the build.</summary>
        private const string Break = "\n";

        private readonly StringBuilder _text = new StringBuilder(96);
        private readonly int[] _lastCharges = new int[SkillLoadout.SlotCount];
        private readonly int[] _lastTenths = new int[SkillLoadout.SlotCount];

        private int _lastHealth = -1;
        private int _lastEnemies = -1;
        private int _lastArena = -1;
        private int _lastBombs = -1;
        private int _lastBombTenths = -1;

        /// <summary>Which arena of the run this is. Zero hides it.</summary>
        public int ArenaNumber { get; set; }

        /// <summary>Refreshes the readout if anything it shows has changed.</summary>
        public void Render(GameSimulation simulation)
        {
            if (_output == null)
            {
                return;
            }

            EnsureNothingIsClipped();

            var health = simulation.State.Player.Health.Current;
            var enemies = simulation.State.Enemies.AliveCount;
            var bombs = BombsInHand(simulation);
            var bombTenths = Tenths(TicksUntilABombReturns(
                bombs, SoonestFuse(simulation), simulation.State.Player.BombCooldownTicksRemaining));

            // Rebuilding a string every frame would allocate for no reason; almost every frame
            // shows exactly what the last one did.
            if (health == _lastHealth && enemies == _lastEnemies && ArenaNumber == _lastArena &&
                bombs == _lastBombs && bombTenths == _lastBombTenths && SkillsUnchanged(simulation))
            {
                return;
            }

            _lastHealth = health;
            _lastEnemies = enemies;
            _lastArena = ArenaNumber;
            _lastBombs = bombs;
            _lastBombTenths = bombTenths;

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

            AppendBombs(bombs, bombTenths);

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
                    AppendSeconds(Tenths(slot));
                    continue;
                }

                _text.Append('-');
            }

            AppendBuild(simulation);

            _output.text = _text.ToString();
        }

        /// <summary>
        /// Writes how many bombs are in hand, or how long until one comes back.
        /// </summary>
        /// <remarks>
        /// The same omission the skills had, and it is worth more here because the bomb is the
        /// core verb. Capacity is the classic model — a bomb returns to the player when it
        /// detonates — so someone who has placed their only one is pressing a button that does
        /// nothing, with nothing on screen saying why, or for how long. The countdown answers both,
        /// and teaches the rule while it does it.
        /// </remarks>
        private void AppendBombs(int inHand, int tenths)
        {
            _text.Append("    BOMBS ");

            if (tenths > 0)
            {
                AppendSeconds(tenths);
                return;
            }

            _text.Append(inHand);
        }

        /// <summary>
        /// Writes tenths of a second as <c>1.4s</c>.
        /// </summary>
        /// <remarks>
        /// Shared by the bombs and the skills so the two countdowns cannot come to disagree about
        /// what a second looks like.
        /// </remarks>
        private void AppendSeconds(int tenths) =>
            _text.Append(tenths / 10).Append('.').Append(tenths % 10).Append('s');

        /// <summary>
        /// Writes out what the player is carrying, on a line of its own.
        /// </summary>
        /// <remarks>
        /// <para>
        /// On screen because the slice measures whether players can describe their build
        /// unprompted. A build they cannot see is one they cannot describe.
        /// </para>
        /// <para>
        /// Its own line because on one line it did not fit. At its longest — a double-digit arena,
        /// a full enemy count, three counters running and two roomy item names — the readout wanted
        /// about 2320 canvas units against the 1840 a 16:9 phone has, and the tablet is narrower
        /// still at 1740. Text runs off the right, so the part that vanished was the end of the
        /// line: the build itself, and nothing else. Measured by
        /// <c>GreyboxScreenLegibilityTests.TheMatchReadoutFitsEveryScreenAtItsLongest</c>.
        /// </para>
        /// </remarks>
        private void AppendBuild(GameSimulation simulation)
        {
            var items = simulation.State.Player.Items;
            var first = true;

            for (var index = 0; index < items.Capacity; index++)
            {
                var id = items[index];

                if (id == ItemId.None)
                {
                    continue;
                }

                _text.Append(first ? Break : "    ").Append(ItemCatalog.Name(id));
                first = false;
            }
        }

        /// <summary>
        /// Stops the box from cutting anything off, however it was authored.
        /// </summary>
        /// <remarks>
        /// The readout grew as skills and the build were added to it, and the authored box did not.
        /// With the default wrapping it folded where it liked and the box clipped what fell out, so
        /// on device the charges and the whole build were being drawn off-screen. Overflow puts the
        /// breaks where this class asks for them instead. Enforced here rather than in the scene so
        /// it holds however the text was authored.
        /// </remarks>
        private void EnsureNothingIsClipped()
        {
            if (_output == null || _output.horizontalOverflow == HorizontalWrapMode.Overflow)
            {
                return;
            }

            _output.horizontalOverflow = HorizontalWrapMode.Overflow;
            _output.verticalOverflow = VerticalWrapMode.Overflow;
        }

        /// <summary>
        /// Ticks until the player may place another bomb.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two separate things can stand in the way and both have to clear: a bomb has to come
        /// back, which under the capacity model means waiting out the soonest fuse on the board,
        /// and any placement cooldown has to expire. The wait is therefore whichever of them ends
        /// last, not the two added together.
        /// </para>
        /// <para>
        /// It is an upper bound on purpose. A chain detonation can return a bomb well before its
        /// own fuse runs out, and a readout that comes back early is a great deal better than one
        /// that promised a shorter wait than it delivered.
        /// </para>
        /// <para>
        /// Stated here rather than inlined because it is the one judgement on this readout: what
        /// the number means when there is nothing in hand.
        /// </para>
        /// </remarks>
        /// <param name="inHand">Bombs the player may still place.</param>
        /// <param name="soonestFuseTicks">The shortest fuse burning on the board, or zero.</param>
        /// <param name="cooldownTicksRemaining">Ticks left on the placement cooldown.</param>
        public static int TicksUntilABombReturns(
            int inHand, int soonestFuseTicks, int cooldownTicksRemaining) =>
            Math.Max(inHand > 0 ? 0 : soonestFuseTicks, cooldownTicksRemaining);

        /// <summary>How many bombs the player may still place.</summary>
        private static int BombsInHand(GameSimulation simulation) =>
            Math.Max(0, simulation.State.Player.BombCapacity - simulation.State.Player.ActiveBombs);

        /// <summary>
        /// The shortest fuse still burning, or zero when the board is clear.
        /// </summary>
        /// <remarks>
        /// Every bomb on the board is the player's — nothing else places one — so no ownership
        /// check is needed here. That stops being true the moment an enemy can lay a bomb.
        /// </remarks>
        private static int SoonestFuse(GameSimulation simulation)
        {
            var soonest = 0;

            for (var index = 0; index < simulation.State.Bombs.Capacity; index++)
            {
                var bomb = simulation.State.Bombs[index];

                if (bomb.IsActive && (soonest == 0 || bomb.FuseTicksRemaining < soonest))
                {
                    soonest = bomb.FuseTicksRemaining;
                }
            }

            return soonest;
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
        private static int Tenths(in SkillSlot slot) => Tenths(slot.CooldownRemaining);

        /// <inheritdoc cref="Tenths(in SkillSlot)" />
        private static int Tenths(int ticks) => ticks * 10 / SimulationConstants.TicksPerSecond;

        private static string Label(SkillId id) => id switch
        {
            SkillId.Dash => "DASH",
            SkillId.Skillshot => "SHOT",
            _ => "SKILL"
        };
    }
}
