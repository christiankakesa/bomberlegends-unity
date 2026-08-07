using System;
using BomberLegends.Simulation.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Run
{
    /// <summary>
    /// The between-arena screen: pick an item, or restart after dying.
    /// </summary>
    /// <remarks>
    /// Built at runtime, like every other view in the greybox. That keeps the scene free of wiring
    /// that would only be thrown away when the real interface arrives, and it means no scene has to
    /// be regenerated to try the loop.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class RunOverlayView : MonoBehaviour
    {
        private static readonly Color PanelColour = new Color(0.05f, 0.05f, 0.09f, 0.82f);
        private static readonly Color ButtonColour = new Color(0.16f, 0.42f, 0.52f);

        private readonly Button[] _choices = new Button[Simulation.Run.GameRun.OfferCount];
        private readonly Text[] _choiceLabels = new Text[Simulation.Run.GameRun.OfferCount];
        private readonly ItemId[] _choiceIds = new ItemId[Simulation.Run.GameRun.OfferCount];

        private GameObject? _panel;
        private Text? _title;
        private Button? _restart;

        /// <summary>Raised with the item the player picked.</summary>
        public event Action<ItemId>? Chosen;

        /// <summary>Raised when the player asks for a fresh run.</summary>
        public event Action? Restarted;

        /// <summary>Whether the overlay is currently covering the game.</summary>
        public bool IsShowing => _panel != null && _panel.activeSelf;

        /// <summary>Builds the overlay under the given canvas, hidden.</summary>
        public void Build(Canvas canvas)
        {
            if (_panel != null)
            {
                return;
            }

            _panel = CreateStretched("RunOverlay", canvas.transform);
            AddImage(_panel, PanelColour);

            _title = CreateLabel(_panel.transform, "ARENA CLEARED", 34, new Vector2(0f, 150f));

            for (var i = 0; i < _choices.Length; i++)
            {
                var offset = new Vector2((i - 1) * 260f, 20f);
                var button = CreateButton(_panel.transform, offset, new Vector2(240f, 90f));

                var index = i;
                button.onClick.AddListener(() => Chosen?.Invoke(_choiceIds[index]));

                _choices[i] = button;
                _choiceLabels[i] = button.GetComponentInChildren<Text>();
            }

            _restart = CreateButton(_panel.transform, new Vector2(0f, -110f), new Vector2(300f, 90f));
            _restart.GetComponentInChildren<Text>().text = "RESTART";
            _restart.onClick.AddListener(() => Restarted?.Invoke());

            _panel.SetActive(false);
        }

        /// <summary>Shows the item choice.</summary>
        public void ShowChoices(ReadOnlySpan<ItemId> offers, int arenaNumber)
        {
            if (_panel == null || _title == null)
            {
                return;
            }

            _title.text = $"ARENA {arenaNumber} CLEARED — CHOOSE ONE";

            for (var i = 0; i < _choices.Length; i++)
            {
                var has = i < offers.Length;

                _choices[i].gameObject.SetActive(has);

                if (has)
                {
                    _choiceIds[i] = offers[i];
                    _choiceLabels[i].text = ItemCatalog.Name(offers[i]);
                }
            }

            _restart?.gameObject.SetActive(false);
            _panel.SetActive(true);
        }

        /// <summary>Shows the death screen.</summary>
        public void ShowEnded(int arenaNumber)
        {
            if (_panel == null || _title == null)
            {
                return;
            }

            _title.text = $"DIED ON ARENA {arenaNumber}";

            for (var i = 0; i < _choices.Length; i++)
            {
                _choices[i].gameObject.SetActive(false);
            }

            _restart?.gameObject.SetActive(true);
            _panel.SetActive(true);
        }

        /// <summary>Hides the overlay and returns control to the match.</summary>
        public void Hide() => _panel?.SetActive(false);

        private static GameObject CreateStretched(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);

            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return child;
        }

        private static void AddImage(GameObject target, Color colour)
        {
            var image = target.AddComponent<Image>();
            image.color = colour;
        }

        private static Text CreateLabel(Transform parent, string text, int size, Vector2 position)
        {
            var child = new GameObject("Label", typeof(RectTransform));
            child.transform.SetParent(parent, false);

            var rect = child.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900f, 90f);

            var label = child.AddComponent<Text>();
            label.text = text;
            label.font = PlaceholderFont();
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            return label;
        }

        private static Button CreateButton(Transform parent, Vector2 position, Vector2 size)
        {
            var child = new GameObject("Button", typeof(RectTransform));
            child.transform.SetParent(parent, false);

            var rect = child.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            AddImage(child, ButtonColour);

            var button = child.AddComponent<Button>();
            CreateLabel(child.transform, string.Empty, 22, Vector2.zero).rectTransform.sizeDelta = size;

            return button;
        }

        private static Font PlaceholderFont() =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
