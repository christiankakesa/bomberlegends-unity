using BomberLegends.Core;
using BomberLegends.Input;
using BomberLegends.Services;
using BomberLegends.Simulation.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Ui
{
    /// <summary>
    /// Builds the on-screen skill cluster: one drag-to-aim button per equipped skill, and the
    /// outline of each slot still to come.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Laid out in an arc inside thumb reach of the bomb button, and built at run time so no scene
    /// has to be regenerated to change it.
    /// </para>
    /// <para>
    /// An empty slot used to be left out entirely, on the rule that a dead control in the middle of
    /// a thumb cluster is worse than a gap. The rule stands and the conclusion was wrong: what is
    /// drawn now is not a control. It takes no touches and carries no
    /// <see cref="SkillTouchButton"/> — it is the <i>shape</i> of one, dimmed and labelled. The
    /// gap said the cluster was finished at two, testers read that as the ceiling and asked why it
    /// was there, and an unexplained ceiling is a worse thing to leave on screen than an empty
    /// place with a name on it.
    /// </para>
    /// </remarks>
    public static class TouchControlsBuilder
    {
        private static readonly Color SkillColour = new Color(0.20f, 0.38f, 0.58f, 0.85f);
        private static readonly Color IndicatorColour = new Color(0.55f, 0.85f, 1f, 0.55f);
        private static readonly Color CancelColour = new Color(0.55f, 0.16f, 0.20f, 0.80f);
        private static readonly Color CooldownColour = new Color(0.02f, 0.03f, 0.06f, 0.72f);

        /// <summary>The skill colour, drained, so an empty slot reads as the same family of thing.</summary>
        private static readonly Color LockedColour = new Color(0.20f, 0.38f, 0.58f, 0.30f);

        /// <summary>Diameter of a skill button, in canvas units.</summary>
        private const float ButtonSize = 130f;

        /// <summary>
        /// The word on a control, at the legibility floor. See <see cref="TextLegibility"/>.
        /// </summary>
        /// <remarks>
        /// These were 20, about 8 dp. Touch is the one input where the label is the only thing
        /// naming the control — there is no key to recognise and no button glyph to learn — so an
        /// unreadable one leaves a coloured circle that does something unknown.
        /// </remarks>
        private const int LabelSize = TextLegibility.MinimumBodySize;

        /// <summary>
        /// Where each slot sits relative to the bomb button, measured from that button's own size.
        /// </summary>
        /// <remarks>
        /// Derived rather than fixed because the bomb button is authored in the scene and can be
        /// resized. Fixed pixel offsets put the first skill straight through it the moment it grew,
        /// which is exactly what the first device build showed.
        /// </remarks>
        private static Vector2[] OffsetsFor(RectTransform anchor)
        {
            var halfWidth = anchor.sizeDelta.x * 0.5f;
            var height = anchor.sizeDelta.y;
            var gap = ButtonSize * 0.5f + 30f;

            return new[]
            {
                new Vector2(-(halfWidth + gap), height * 0.05f),
                new Vector2(-(halfWidth + gap * 0.7f), height * 0.95f),
                new Vector2(-(halfWidth - gap * 0.35f), height * 1.75f)
            };
        }

        /// <summary>Creates a button for every equipped skill, and an outline for every empty slot.</summary>
        /// <param name="anchor">The bomb button the cluster is laid out around.</param>
        /// <param name="loadout">The skills the player is carrying.</param>
        /// <param name="lockedSlots">
        /// The outlines drawn for slots holding no skill. Handed back rather than forgotten because
        /// they are part of the cluster and have to leave the screen with it: the controls stand
        /// down while a choice screen or the pause menu is up, and anything left behind would be
        /// drawn over the decision.
        /// </param>
        public static SkillTouchButton[] Build(
            RectTransform anchor, in SkillLoadout loadout, out GameObject[] lockedSlots)
        {
            var offsets = OffsetsFor(anchor);
            var cancel = CreateCancelZone(anchor, offsets);
            var buttons = new System.Collections.Generic.List<SkillTouchButton>(SkillLoadout.SlotCount);
            var locked = new System.Collections.Generic.List<GameObject>(SkillLoadout.SlotCount);

            for (var slot = 0; slot < SkillLoadout.SlotCount && slot < offsets.Length; slot++)
            {
                var skill = loadout[slot];

                if (!skill.IsEquipped)
                {
                    locked.Add(CreateLockedSlot(anchor, offsets[slot]));
                    continue;
                }

                buttons.Add(CreateSkillButton(anchor, offsets[slot], slot, skill.Id, cancel));
            }

            lockedSlots = locked.ToArray();
            return buttons.ToArray();
        }

        private static SkillTouchButton CreateSkillButton(
            RectTransform anchor, Vector2 offset, int slot, SkillId id, RectTransform cancel)
        {
            var host = new GameObject($"Skill{slot + 1}Button", typeof(RectTransform));
            host.transform.SetParent(anchor.parent, false);

            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = anchor.anchorMin;
            rect.anchorMax = anchor.anchorMax;
            rect.pivot = anchor.pivot;
            rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            rect.anchoredPosition = anchor.anchoredPosition + offset;

            host.AddComponent<Image>().color = SkillColour;

            GreyboxUi.CreateLabel(host.transform, Label(id), LabelSize, Vector2.zero, rect.sizeDelta);

            // Drawn behind the button and pointing away from it, so a thumb never covers the part
            // of the indicator that says where the shot is going.
            var indicator = CreateIndicator(rect);

            var cooldown = CreateCooldownOverlay(rect);

            var button = host.AddComponent<SkillTouchButton>();
            button.Initialise(ButtonFor(slot), indicator, cancel, cooldown);

            return button;
        }

        /// <summary>
        /// Draws a slot that is not a control yet.
        /// </summary>
        /// <remarks>
        /// Explicitly not a raycast target. A thumb that lands here has to reach the board
        /// underneath, or the promise would cost the player the tap they actually meant — which is
        /// the defect the choice screen just spent two fixes on.
        /// </remarks>
        private static GameObject CreateLockedSlot(RectTransform anchor, Vector2 offset)
        {
            var host = new GameObject("LockedSlot", typeof(RectTransform));
            host.transform.SetParent(anchor.parent, false);

            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = anchor.anchorMin;
            rect.anchorMax = anchor.anchorMax;
            rect.pivot = anchor.pivot;
            rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            rect.anchoredPosition = anchor.anchoredPosition + offset;

            var image = host.AddComponent<Image>();
            image.color = LockedColour;
            image.raycastTarget = false;

            // A wider box than the circle it sits on: the word is longer than DASH or SHOT, and at
            // the legibility floor it does not fit inside 130 units. The floor does not move.
            GreyboxUi.CreateLabel(
                host.transform, "LOCKED", LabelSize, Vector2.zero, new Vector2(ButtonSize * 1.7f, 60f));

            return host;
        }

        private static RectTransform CreateIndicator(RectTransform parent)
        {
            var host = new GameObject("AimIndicator", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            var rect = host.GetComponent<RectTransform>();

            // Pivoted at its base so scaling grows it outward from the button.
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(26f, 220f);

            var image = host.AddComponent<Image>();
            image.color = IndicatorColour;
            image.raycastTarget = false;

            host.transform.SetAsFirstSibling();
            host.SetActive(false);

            return rect;
        }

        /// <summary>
        /// A dark pane over the button that shrinks away as the skill recharges.
        /// </summary>
        /// <remarks>
        /// Scaled from the top rather than filled radially, so it needs no sprite and no atlas — the
        /// greybox has neither. What is still covered is what is still to wait.
        /// </remarks>
        private static RectTransform CreateCooldownOverlay(RectTransform parent)
        {
            var host = new GameObject("Cooldown", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 1f);

            var image = host.AddComponent<Image>();
            image.color = CooldownColour;

            // Never a touch target: the button underneath must stay pressable while it recharges,
            // so a player can learn that pressing early does nothing rather than that it is broken.
            image.raycastTarget = false;

            host.SetActive(false);

            return rect;
        }

        private static RectTransform CreateCancelZone(RectTransform anchor, Vector2[] offsets)
        {
            var host = new GameObject("CancelZone", typeof(RectTransform));
            host.transform.SetParent(anchor.parent, false);

            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = anchor.anchorMin;
            rect.anchorMax = anchor.anchorMax;
            rect.pivot = anchor.pivot;
            rect.sizeDelta = new Vector2(240f, 140f);

            // Above the topmost skill, so a drag away from the cluster reaches it naturally and a
            // thumb resting on the buttons never sits inside it.
            rect.anchoredPosition =
                anchor.anchoredPosition + offsets[offsets.Length - 1] + new Vector2(0f, 190f);

            var image = host.AddComponent<Image>();
            image.color = CancelColour;

            // Never a click target: it is a region a drag is released over, not a button.
            image.raycastTarget = false;

            GreyboxUi.CreateLabel(host.transform, "CANCEL", LabelSize, Vector2.zero, rect.sizeDelta);

            host.SetActive(false);

            return rect;
        }

        private static IntentButtons ButtonFor(int slot) => slot switch
        {
            0 => IntentButtons.Skill1,
            1 => IntentButtons.Skill2,
            _ => IntentButtons.Skill3
        };

        private static string Label(SkillId id) => id switch
        {
            SkillId.Dash => "DASH",
            SkillId.Skillshot => "SHOT",
            _ => "SKILL"
        };
    }
}
