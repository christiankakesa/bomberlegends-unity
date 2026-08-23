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
        private static readonly Color StatusColour = new Color(0.62f, 0.68f, 0.76f);
        private static readonly Color TakeColour = new Color(0.98f, 0.86f, 0.42f);

        // Sizes are canvas units against a 1920x1080 reference at match 0.5. The conversion to
        // something a phone can read lives in TextLegibility, and is deferred to rather than
        // restated: this screen was sized twice from a figure written down by hand, and both times
        // the figure was wrong. The first pass used 20 units for a description and shipped it at
        // 8 dp. The second pass corrected that to 32 believing a unit was 0.46 dp — but the device
        // reports 3.75 pixels per dp, not the 3.22 that had been recorded, so 32 was really 12.7 dp
        // and still under the floor. It read as fixed because nobody measured the phone again.
        //
        // What survives that is not a better number but the constant below, checked against the
        // device by ChoiceCardLegibilityTests. Anything at the floor says so by name.
        private const int HeadlineSize = 48;

        // The status line is subordinate to the headline, which the headline's 48 is large enough
        // to carry on its own. Subordinate still is not the same as unreadable, so this sits on the
        // floor rather than below it.
        private const int StatusSize = TextLegibility.MinimumBodySize;
        private const int CardNameSize = 40;

        // The sentence the whole screen exists for. It is the text three testers could not read.
        private const int CardBlurbSize = TextLegibility.MinimumBodySize;
        private const int ButtonSize = TextLegibility.MinimumBodySize;

        // The word that tells a player the card is pressable, and so the last one to skimp on.
        private const int TakeSize = TextLegibility.MinimumBodySize;

        // The safe box is 1920 x 940: at match 0.5 a wider phone gains canvas width and loses
        // height, so height is the dimension that runs out first.
        private static readonly Vector2 CardSize = new Vector2(580f, 480f);
        private const float CardSpacing = 600f;

        private readonly Button[] _choices = new Button[Simulation.Run.GameRun.OfferCount];
        private readonly Text[] _choiceLabels = new Text[Simulation.Run.GameRun.OfferCount];
        private readonly Text[] _choiceBlurbs = new Text[Simulation.Run.GameRun.OfferCount];
        private readonly Text[] _choiceTakes = new Text[Simulation.Run.GameRun.OfferCount];
        private readonly ItemId[] _choiceIds = new ItemId[Simulation.Run.GameRun.OfferCount];

        private GameObject? _panel;
        private Text? _headline;
        private Text? _status;
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

            // Two lines, because they answer different questions. The status says where the run is;
            // the headline says what the screen wants. Testers read this moment as a results screen
            // and picked at random, so the instruction is the larger of the two on purpose.
            _status = CreateLabel(_panel.transform, string.Empty, StatusSize, new Vector2(0f, 420f));
            _status.color = StatusColour;

            _headline = CreateLabel(_panel.transform, "CHOOSE ONE", HeadlineSize, new Vector2(0f, 355f));

            for (var i = 0; i < _choices.Length; i++)
            {
                var offset = new Vector2((i - 1) * CardSpacing, 70f);
                var button = CreateCard(_panel.transform, offset, CardSize,
                    out var name, out var blurb, out var take);

                var index = i;
                button.onClick.AddListener(() => OnPressed(index));

                _choices[i] = button;
                _choiceLabels[i] = name;
                _choiceBlurbs[i] = blurb;
                _choiceTakes[i] = take;
            }

            _skip = CreateButton(_panel.transform, new Vector2(0f, -300f), new Vector2(280f, 80f));
            _skip.GetComponentInChildren<Text>().text = "SKIP";
            _skip.onClick.AddListener(() => Skipped?.Invoke());

            _restart = CreateButton(_panel.transform, new Vector2(0f, -120f), new Vector2(360f, 110f));
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
            if (_panel == null || _headline == null || _status == null)
            {
                return;
            }

            _discarding = false;
            _status.text = $"ARENA {arenaNumber} CLEARED";
            _headline.text = "CHOOSE ONE";

            FillButtons(offers);

            _skip?.gameObject.SetActive(true);
            _restart?.gameObject.SetActive(false);
            _panel.SetActive(true);

            FocusFirstChoice();
        }

        /// <summary>Shows which held item to give up for the one being taken.</summary>
        public void ShowDiscard(ReadOnlySpan<ItemId> held, ItemId taking)
        {
            if (_panel == null || _headline == null || _status == null)
            {
                return;
            }

            _discarding = true;
            _status.text = $"TAKING {ItemCatalog.Name(taking)}";
            _headline.text = "GIVE UP WHICH?";

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

                    // "GIVE UP" while discarding: the same card in the same place means the
                    // opposite thing, and a player who has stopped reading the headline by the
                    // third arena would otherwise throw away the item they meant to keep.
                    _choiceTakes[i].text = _discarding ? "GIVE UP" : "TAKE";
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
            if (_panel == null || _headline == null || _status == null)
            {
                return;
            }

            _discarding = false;
            _status.text = string.Empty;
            _headline.text = $"DIED ON ARENA {arenaNumber}";

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
        /// Builds a choice card: a name you can scan, a sentence explaining what it changes, and a
        /// word saying what pressing it does.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The sentence is the point. A name alone tells a player nothing, and the slice measures
        /// whether they choose deliberately — which they cannot do from "BOMB TRAIL".
        /// </para>
        /// <para>
        /// The footer is the second point, and it is what three touch testers were missing. A card
        /// that only shows information reads as a results screen; the word <c>TAKE</c> is what turns
        /// it into a control, and it costs one label to say so on every input device at once.
        /// </para>
        /// </remarks>
        private static Button CreateCard(
            Transform parent, Vector2 position, Vector2 size,
            out Text name, out Text blurb, out Text take)
        {
            var button = CreateButton(parent, position, size);

            name = CreateLabel(button.transform, string.Empty, CardNameSize, new Vector2(0f, size.y * 0.34f));
            name.rectTransform.sizeDelta = new Vector2(size.x - 40f, 96f);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;

            blurb = CreateLabel(button.transform, string.Empty, CardBlurbSize, new Vector2(0f, -size.y * 0.04f));
            blurb.rectTransform.sizeDelta = new Vector2(size.x - 44f, size.y * 0.5f);
            blurb.alignment = TextAnchor.UpperCenter;
            blurb.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Overflow rather than Truncate. Truncating hid the end of a description with no sign
            // that anything was missing, which is the worst way for this to fail: the card still
            // looks complete. The sizes above leave room for the longest description in the
            // catalog, and a test holds new ones to that budget rather than trusting it.
            blurb.verticalOverflow = VerticalWrapMode.Overflow;
            blurb.color = new Color(0.85f, 0.90f, 0.95f);

            take = CreateLabel(button.transform, "TAKE", TakeSize, new Vector2(0f, -size.y * 0.39f));
            take.rectTransform.sizeDelta = new Vector2(size.x - 40f, 56f);
            take.color = TakeColour;

            return button;
        }

        private static Text CreateLabel(Transform parent, string text, int size, Vector2 position) =>
            GreyboxUi.CreateLabel(parent, text, size, position);

        // The size is still passed explicitly, though GreyboxUi now defaults to the same floor.
        // Saying it here keeps the card's own sizes readable as one set rather than leaving one of
        // them to be inferred from a default somewhere else.
        private static Button CreateButton(Transform parent, Vector2 position, Vector2 size) =>
            GreyboxUi.CreateButton(parent, string.Empty, position, size, ButtonColour, ButtonSize);

    }
}
