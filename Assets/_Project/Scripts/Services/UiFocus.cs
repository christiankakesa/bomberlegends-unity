using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BomberLegends.Services
{
    /// <summary>
    /// Keeps a control selected, so a gamepad or the keyboard always has somewhere to start.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unity's UI navigation moves <i>from</i> whatever is currently selected. With nothing
    /// selected — which is the state every screen loads in — a d-pad does nothing at all and the
    /// interface looks broken to anyone not holding a mouse. The input module was already correct;
    /// only the starting point was missing.
    /// </para>
    /// <para>
    /// Just as important is <see cref="Clear"/>. On a pad, Submit is the same physical button as
    /// Bomb, so anything left selected during a match would be clicked every time the player throws
    /// a bomb — including the control that abandons the match. Selection belongs to menus, and must
    /// be given up the moment play resumes.
    /// </para>
    /// </remarks>
    public static class UiFocus
    {
        /// <summary>What is selected right now, if anything.</summary>
        public static GameObject? Current =>
            EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        /// <summary>Selects a control, or nothing when given null.</summary>
        public static void Select(GameObject? target)
        {
            var events = EventSystem.current;
            if (events == null)
            {
                return;
            }

            events.SetSelectedGameObject(target);
        }

        /// <summary>Gives up selection entirely, so no button can be triggered by Submit.</summary>
        public static void Clear() => Select(null);

        /// <summary>
        /// Selects the first control beneath <paramref name="root"/> that can actually take it.
        /// </summary>
        /// <returns>Whether anything was selectable.</returns>
        public static bool SelectFirstIn(GameObject? root)
        {
            if (root == null)
            {
                return false;
            }

            var candidates = root.GetComponentsInChildren<Selectable>(includeInactive: false);

            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];

                if (!candidate.interactable || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Select(candidate.gameObject);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Colours a control so its focused state is unmistakable.
        /// </summary>
        /// <remarks>
        /// A mouse user knows what they are about to click because the pointer is on it. A pad user
        /// has only the highlight, so it has to be loud — the default tint is far too subtle to
        /// navigate by.
        /// </remarks>
        public static void ApplyNavigationColours(Selectable selectable, Color baseColour)
        {
            var colours = selectable.colors;

            colours.normalColor = baseColour;
            colours.highlightedColor = Brighten(baseColour, 0.35f);
            colours.selectedColor = Brighten(baseColour, 0.55f);
            colours.pressedColor = Brighten(baseColour, -0.2f);
            colours.disabledColor = Brighten(baseColour, -0.45f);
            colours.fadeDuration = 0.08f;

            selectable.colors = colours;
        }

        private static Color Brighten(Color colour, float amount) => amount >= 0f
            ? Color.Lerp(colour, Color.white, amount)
            : Color.Lerp(colour, Color.black, -amount);
    }
}
