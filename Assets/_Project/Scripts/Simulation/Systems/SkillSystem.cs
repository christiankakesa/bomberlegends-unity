using BomberLegends.Core;
using BomberLegends.Simulation.Events;
using BomberLegends.Simulation.Skills;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Recharges the player's active skills and turns button presses into effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs before movement, so a dash pressed this tick moves the player this tick. Any later and
    /// the input would cost a frame of latency on the one action whose whole value is immediacy.
    /// </para>
    /// <para>
    /// Dispatch is a switch on <see cref="SkillId"/> and nothing more. Every number a skill uses
    /// comes from its <see cref="SkillSlot"/>, which lives in simulation state, so an item can
    /// change what a skill does by writing to that slot — no branch here needs to know items exist.
    /// </para>
    /// </remarks>
    public static class SkillSystem
    {
        /// <summary>Advances every skill slot by one tick.</summary>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            in PlayerIntent intent,
            SimEventBuffer events)
        {
            for (var index = 0; index < SkillLoadout.SlotCount; index++)
            {
                var slot = state.Player.Skills[index];

                if (!slot.IsEquipped)
                {
                    continue;
                }

                slot.TickCooldown();

                var held = intent.Buttons.Has(SkillLoadout.ButtonFor(index));
                var pressed = held && !slot.HeldLastTick;
                slot.HeldLastTick = held;

                // Write the slot back before running the effect: firing a skillshot reads player
                // state, and leaving a stale copy in flight is exactly how cooldowns get lost.
                state.Player.Skills[index] = slot;

                if (!pressed || !slot.IsReady)
                {
                    continue;
                }

                if (!TryUse(ref state, config, intent, slot, index, events))
                {
                    continue;
                }

                slot.TrySpend();
                state.Player.Skills[index] = slot;

                events.Add(new SimEvent(
                    SimEventType.SkillUsed, state.Player.Tile, index, (int)slot.Id));
            }
        }

        /// <summary>Runs one skill, returning whether it actually happened.</summary>
        /// <remarks>
        /// A skill that cannot take effect must not consume its charge. Spending a dash on nothing
        /// because the projectile pool was full is the kind of silent theft players never forgive.
        /// </remarks>
        private static bool TryUse(
            ref SimulationState state,
            in SimulationConfig config,
            in PlayerIntent intent,
            in SkillSlot slot,
            int index,
            SimEventBuffer events) => slot.Id switch
        {
            SkillId.Dash => TryDash(ref state, config, intent, slot, events),
            SkillId.Skillshot => TryFire(ref state, config, intent, slot, events),
            _ => false
        };

        private static bool TryDash(
            ref SimulationState state,
            in SimulationConfig config,
            in PlayerIntent intent,
            in SkillSlot slot,
            SimEventBuffer events)
        {
            // A dash follows the stick, not the aim. Every twin-stick game resolves it that way and
            // for the same reason: a pad player holding the right stick on an enemy is lining up a
            // shot, and a dash that obeyed it would launch them into the thing they were escaping.
            //
            // The exception is an aim drawn *for this skill*. On touch every skill button is its
            // own stick (07-CONCEPT-REVISION §4i), so a drag on the dash button is the only
            // direction the player gave — and until this flag existed the simulation discarded it,
            // leaving the gesture, its on-button indicator and the ground arrow all pointing
            // somewhere the dash would not go.
            var aimed = intent.Buttons.Has(IntentButtons.AimedCast) && intent.HasAim;

            if (!TryResolveHeading(
                    aimed ? intent.AimX : intent.MoveX,
                    aimed ? intent.AimY : intent.MoveY,
                    state.Player.Facing,
                    state.Player.LastHeadingX,
                    state.Player.LastHeadingY,
                    config.DirectionDeadzone,
                    slot.Magnitude,
                    out var velocityX,
                    out var velocityY))
            {
                return false;
            }

            // Laid before the dash carries the player away, so the bomb marks where they left
            // rather than where they arrived — which is what makes it a trap instead of a suicide.
            if (slot.Traits.Has(SkillTraits.LeavesBombs))
            {
                BombPlacementSystem.TryPlace(ref state, config, events);
            }

            state.Player.DashTicksRemaining = slot.DurationTicks;
            state.Player.DashVelocityX = velocityX;
            state.Player.DashVelocityY = velocityY;
            state.Player.DashTraits = slot.Traits;
            state.Player.DashPower = slot.Power;

            events.Add(new SimEvent(
                SimEventType.DashStarted, state.Player.Tile, 0, slot.DurationTicks));

            return true;
        }

        private static bool TryFire(
            ref SimulationState state,
            in SimulationConfig config,
            in PlayerIntent intent,
            in SkillSlot slot,
            SimEventBuffer events)
        {
            // Aim wins when it is supplied; otherwise the shot follows the stick, and failing that
            // whichever way the player is looking. Keyboard-only play never has an aim axis, so
            // without this fallback the skill would simply not exist on a keyboard.
            var aimX = intent.HasAim ? intent.AimX : intent.MoveX;
            var aimY = intent.HasAim ? intent.AimY : intent.MoveY;

            if (!TryResolveHeading(
                    aimX,
                    aimY,
                    state.Player.Facing,
                    state.Player.LastHeadingX,
                    state.Player.LastHeadingY,
                    config.DirectionDeadzone,
                    slot.Magnitude,
                    out var velocityX,
                    out var velocityY))
            {
                return false;
            }

            var projectile = state.Projectiles.Fire(
                state.Player.Position,
                velocityX,
                velocityY,
                slot.DurationTicks,
                slot.Power,
                slot.Traits);

            if (projectile < 0)
            {
                return false;
            }

            events.Add(new SimEvent(
                SimEventType.ProjectileFired, state.Player.Tile, projectile, slot.Power));

            return true;
        }

        /// <summary>
        /// Turns a stick reading into a velocity of the given speed, falling back to where the
        /// player was last actually going, and only then to which way they face.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The order of those fallbacks is the fix for "the dash went in a different direction than
        /// I thought". A stick returns to centre faster than a thumb reaches a button, so a player
        /// running diagonally routinely presses dash on a frame where the stick reads nothing. With
        /// only a cardinal facing to fall back on, the dash was thrown up to 45° off the line they
        /// were travelling — reliably, and with no way for them to see why.
        /// </para>
        /// <para>
        /// The recorded heading is exact, so the dash follows the diagonal. Facing survives as the
        /// last resort for a player who has not moved at all yet.
        /// </para>
        /// <para>
        /// Integer normalisation, as everywhere else in the simulation: a floating-point square root
        /// is not bit-identical across platforms, and a single differing bit in a dash velocity
        /// would put two runs of the same replay in different places.
        /// </para>
        /// </remarks>
        private static bool TryResolveHeading(
            int x,
            int y,
            Direction facing,
            int lastHeadingX,
            int lastHeadingY,
            int deadzone,
            int speed,
            out int velocityX,
            out int velocityY)
        {
            velocityX = 0;
            velocityY = 0;

            if (speed <= 0)
            {
                return false;
            }

            if ((x * x) + (y * y) < deadzone * deadzone)
            {
                x = lastHeadingX;
                y = lastHeadingY;
            }

            if (x == 0 && y == 0)
            {
                var offset = facing.ToOffset();
                x = offset.X * PlayerIntent.AxisRange;
                y = offset.Y * PlayerIntent.AxisRange;

                if (x == 0 && y == 0)
                {
                    return false;
                }
            }

            var length = IntMath.Sqrt((x * x) + (y * y));

            if (length <= 0)
            {
                return false;
            }

            velocityX = x * speed / length;
            velocityY = y * speed / length;

            return velocityX != 0 || velocityY != 0;
        }
    }
}
