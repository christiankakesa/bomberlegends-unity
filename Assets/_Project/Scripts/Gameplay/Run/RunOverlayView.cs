using System;
using BomberLegends.Gameplay.Ui;
using BomberLegends.Services;
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
        private readonly Text[] _choiceBlurbs = new Text[Simulation.Run.GameRun.OfferCount];
        private readonly ItemId[] _choiceIds = new ItemId[Simulation.Run.GameRun.OfferCount];

        private GameObject? _panel;
        private Text? _title;
        private Button? _restart;
        private Button? _skip;
        private bool _discarding;

        /// <summary>Raised with the item the player picked.</summary>
        public event Action<ItemId>? Chosen;

        /// <summary>Raised with the held item the player gave up.</summary>
        public event Action<ItemId>? Discarded;

        /// <summary>Raised when the player declines the offer.</summary>
        public event Action? Skipped;

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

            _panel = GreyboxUi.CreateFullScreenPanel("RunOverlay", canvas.transform, PanelColour);

            _title = CreateLabel(_panel.transform, "ARENA CLEARED", 34, new Vector2(0f, 150f));

            for (var i = 0; i < _choices.Length; i++)
            {
                var offset = new Vector2((i - 1) * 350f, 30f);
                var button = CreateCard(_panel.transform, offset, new Vector2(330f, 270f),
                    out var name, out var blurb);

                var index = i;
                button.onClick.AddListener(() => OnPressed(index));

                _choices[i] = button;
                _choiceLabels[i] = name;
                _choiceBlurbs[i] = blurb;
            }

            _skip = CreateButton(_panel.transform, new Vector2(0f, -170f), new Vector2(220f, 60f));
            _skip.GetComponentInChildren<Text>().text = "SKIP";
            _skip.onClick.AddListener(() => Skipped?.Invoke());

            _restart = CreateButton(_panel.transform, new Vector2(0f, -110f), new Vector2(300f, 90f));
            _restart.GetComponentInChildren<Text>().text = "RESTART";
            _restart.onClick.AddListener(() => Restarted?.Invoke());

            // Hidden first, then given its keeper. Attaching it to a live object would fire
            // OnEnable and grab focus before the overlay is ever shown.
            _panel.SetActive(false);

            // Lives on the panel, so it runs only while the overlay is up. Selection must be gone
            // during a match: on a pad, Submit and Bomb are the same button.
            _panel.AddComponent<UiFocusKeeper>();
        }

        /// <summary>Shows the item choice.</summary>
        public void ShowChoices(ReadOnlySpan<ItemId> offers, int arenaNumber)
        {
            if (_panel == null || _title == null)
            {
                return;
            }

            _discarding = false;
            _title.text = $"ARENA {arenaNumber} CLEARED — CHOOSE ONE";

            FillButtons(offers);

            _skip?.gameObject.SetActive(true);
            _restart?.gameObject.SetActive(false);
            _panel.SetActive(true);

            FocusFirstChoice();
        }

        /// <summary>Shows which held item to give up for the one being taken.</summary>
        public void ShowDiscard(ReadOnlySpan<ItemId> held, ItemId taking)
        {
            if (_panel == null || _title == null)
            {
                return;
            }

            _discarding = true;
            _title.text = $"TAKING {ItemCatalog.Name(taking)} — GIVE UP WHICH?";

            FillButtons(held);

            _skip?.gameObject.SetActive(true);
            _restart?.gameObject.SetActive(false);
            _panel.SetActive(true);

            FocusFirstChoice();
        }

        private void FillButtons(ReadOnlySpan<ItemId> ids)
        {
            for (var i = 0; i < _choices.Length; i++)
            {
                var has = i < ids.Length;

                _choices[i].gameObject.SetActive(has);

                if (has)
                {
                    _choiceIds[i] = ids[i];
                    _choiceLabels[i].text = ItemCatalog.Name(ids[i]);
                    _choiceBlurbs[i].text = ItemCatalog.Description(ids[i]);
                }
            }
        }

        private void OnPressed(int index)
        {
            if (_discarding)
            {
                Discarded?.Invoke(_choiceIds[index]);
                return;
            }

            Chosen?.Invoke(_choiceIds[index]);
        }

        /// <summary>Shows the death screen.</summary>
        public void ShowEnded(int arenaNumber)
        {
            if (_panel == null || _title == null)
            {
                return;
            }

            _discarding = false;
            _title.text = $"DIED ON ARENA {arenaNumber}";

            for (var i = 0; i < _choices.Length; i++)
            {
                _choices[i].gameObject.SetActive(false);
            }

            _skip?.gameObject.SetActive(false);
            _restart?.gameObject.SetActive(true);
            _panel.SetActive(true);

            SetKeeperFallback(_restart != null ? _restart.gameObject : null);
            UiFocus.Select(_restart != null ? _restart.gameObject : null);
        }

        /// <summary>Hides the overlay and returns control to the match.</summary>
        /// <remarks>
        /// Selection is given up as well as hidden. Leaving a control selected would let the bomb
        /// button press it, because on a pad they are the same button.
        /// </remarks>
        public void Hide()
        {
            _panel?.SetActive(false);
            UiFocus.Clear();
        }

        private void FocusFirstChoice()
        {
            for (var i = 0; i < _choices.Length; i++)
            {
                if (!_choices[i].gameObject.activeSelf)
                {
                    continue;
                }

                SetKeeperFallback(_choices[i].gameObject);
                UiFocus.Select(_choices[i].gameObject);
                return;
            }

            SetKeeperFallback(_skip != null ? _skip.gameObject : null);
            UiFocus.Select(_skip != null ? _skip.gameObject : null);
        }

        private void SetKeeperFallback(GameObject? target)
        {
            if (_panel == null)
            {
                return;
            }

            var keeper = _panel.GetComponent<UiFocusKeeper>();
            if (keeper != null)
            {
                keeper.Fallback = target;
            }
        }

        /// <summary>
        /// Builds a choice card: a name you can scan and a sentence explaining what it changes.
        /// </summary>
        /// <remarks>
        /// The sentence is the point. A name alone tells a player nothing, and the slice measures
        /// whether they choose deliberately — which they cannot do from "BOMB TRAIL".
        /// </remarks>
        private static Button CreateCard(
            Transform parent, Vector2 position, Vector2 size, out Text name, out Text blurb)
        {
            var button = CreateButton(parent, position, size);

            name = CreateLabel(button.transform, string.Empty, 24, new Vector2(0f, size.y * 0.36f));
            name.rectTransform.sizeDelta = new Vector2(size.x - 24f, 56f);

            // Large enough to read at a glance from a normal sitting distance. A description nobody
            // reads is the same as no description, which was the whole problem being fixed.
            blurb = CreateLabel(button.transform, string.Empty, 20, new Vector2(0f, -size.y * 0.08f));
            blurb.rectTransform.sizeDelta = new Vector2(size.x - 32f, size.y * 0.62f);
            blurb.alignment = TextAnchor.UpperCenter;
            blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
            blurb.verticalOverflow = VerticalWrapMode.Truncate;
            blurb.color = new Color(0.85f, 0.90f, 0.95f);

            return button;
        }

        private static Text CreateLabel(Transform parent, string text, int size, Vector2 position) =>
            GreyboxUi.CreateLabel(parent, text, size, position);

        private static Button CreateButton(Transform parent, Vector2 position, Vector2 size) =>
            GreyboxUi.CreateButton(parent, string.Empty, position, size, ButtonColour, fontSize: 22);

    }
}
