using BomberLegends.Core;
using BomberLegends.Input;
using BomberLegends.Simulation.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Ui
{
    /// <summary>
    /// Builds the on-screen skill cluster: one drag-to-aim button per equipped skill.
    /// </summary>
    /// <remarks>
    /// Laid out in an arc inside thumb reach of the bomb button, and built at run time so no scene
    /// has to be regenerated to change it. Slots holding no skill get no button rather than a dead
    /// one — an unusable control in the middle of a thumb cluster is worse than a gap.
    /// </remarks>
    public static class TouchControlsBuilder
    {
        private static readonly Color SkillColour = new Color(0.20f, 0.38f, 0.58f, 0.85f);
        private static readonly Color IndicatorColour = new Color(0.55f, 0.85f, 1f, 0.55f);
        private static readonly Color CancelColour = new Color(0.55f, 0.16f, 0.20f, 0.80f);

        /// <summary>Diameter of a skill button, in canvas units.</summary>
        private const float ButtonSize = 130f;

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

        /// <summary>Creates a button for every equipped skill.</summary>
        public static SkillTouchButton[] Build(RectTransform anchor, in SkillLoadout loadout)
        {
            var offsets = OffsetsFor(anchor);
            var cancel = CreateCancelZone(anchor, offsets);
            var buttons = new System.Collections.Generic.List<SkillTouchButton>(SkillLoadout.SlotCount);

            for (var slot = 0; slot < SkillLoadout.SlotCount && slot < offsets.Length; slot++)
            {
                var skill = loadout[slot];

                if (!skill.IsEquipped)
                {
                    continue;
                }

                buttons.Add(CreateSkillButton(anchor, offsets[slot], slot, skill.Id, cancel));
            }

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

            GreyboxUi.CreateLabel(host.transform, Label(id), 20, Vector2.zero, rect.sizeDelta);

            // Drawn behind the button and pointing away from it, so a thumb never covers the part
            // of the indicator that says where the shot is going.
            var indicator = CreateIndicator(rect);

            var button = host.AddComponent<SkillTouchButton>();
            button.Initialise(ButtonFor(slot), indicator, cancel);

            return button;
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

            GreyboxUi.CreateLabel(host.transform, "CANCEL", 20, Vector2.zero, rect.sizeDelta);

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
