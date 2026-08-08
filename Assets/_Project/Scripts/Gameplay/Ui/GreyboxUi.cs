using BomberLegends.Services;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Ui
{
    /// <summary>
    /// Builds the placeholder interface the greybox runs on.
    /// </summary>
    /// <remarks>
    /// Everything here is created at run time rather than authored in a scene, which is what keeps
    /// the scenes free of wiring that will be thrown away when the real interface arrives — and what
    /// lets a new screen be added without regenerating anything.
    /// </remarks>
    public static class GreyboxUi
    {
        /// <summary>A panel filling its parent.</summary>
        public static GameObject CreateFullScreenPanel(string name, Transform parent, Color colour)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            panel.AddComponent<Image>().color = colour;

            return panel;
        }

        /// <summary>A centred text label.</summary>
        public static Text CreateLabel(
            Transform parent, string text, int size, Vector2 position, Vector2? area = null)
        {
            var child = new GameObject("Label", typeof(RectTransform));
            child.transform.SetParent(parent, false);

            var rect = child.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = area ?? new Vector2(900f, 90f);

            var label = child.AddComponent<Text>();
            label.text = text;
            label.font = Font();
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;

            // Never a click target. A label swallowing a press is invisible until someone reports a
            // button that only works around its edges.
            label.raycastTarget = false;

            return label;
        }

        /// <summary>
        /// A button whose focused state is unmistakable.
        /// </summary>
        /// <remarks>
        /// The navigation colours are not decoration. A mouse user knows what they are about to
        /// click because the pointer sits on it; a pad user has nothing but the highlight.
        /// </remarks>
        public static Button CreateButton(
            Transform parent, string label, Vector2 position, Vector2 size, Color colour, int fontSize = 24)
        {
            var child = new GameObject("Button", typeof(RectTransform));
            child.transform.SetParent(parent, false);

            var rect = child.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            child.AddComponent<Image>().color = colour;

            var button = child.AddComponent<Button>();
            UiFocus.ApplyNavigationColours(button, colour);

            CreateLabel(child.transform, label, fontSize, Vector2.zero, size);

            return button;
        }

        /// <summary>The built-in font, so no asset has to exist for the greybox to draw text.</summary>
        public static Font Font() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
