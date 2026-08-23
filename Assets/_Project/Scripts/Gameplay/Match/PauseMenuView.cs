using System;
using BomberLegends.Gameplay.Ui;
using BomberLegends.Services;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Match
{
    /// <summary>
    /// The paused screen: carry on, or abandon the match.
    /// </summary>
    /// <remarks>
    /// Exists so a match can be left without a mouse. Selection is deliberately cleared on resume:
    /// on a pad, Submit and Bomb are the same physical button, so a control left selected would be
    /// pressed every time the player throws a bomb.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        private static readonly Color PanelColour = new Color(0.04f, 0.05f, 0.10f, 0.86f);
        private static readonly Color ResumeColour = new Color(0.16f, 0.45f, 0.40f);
        private static readonly Color QuitColour = new Color(0.42f, 0.18f, 0.22f);

        /// <summary>The word a player scans for, large enough to be found without being aimed at.</summary>
        private const int HeadlineSize = 40;

        /// <summary>Sizes are canvas units. See <see cref="TextLegibility"/> for the conversion.</summary>
        private const int BodySize = TextLegibility.MinimumBodySize;

        // 360 x 96 rather than 320 x 84. The extra width carries QUIT TO HUB at the larger size
        // without wrapping, and the extra height keeps the line off the edges of its own button.
        private static readonly Vector2 ButtonSize = new Vector2(360f, 96f);

        private GameObject? _panel;
        private Button? _resume;

        /// <summary>Raised when the player wants to carry on.</summary>
        public event Action? Resumed;

        /// <summary>Raised when the player wants to abandon the match.</summary>
        public event Action? Quit;

        /// <summary>Whether the menu is currently covering the match.</summary>
        public bool IsShowing => _panel != null && _panel.activeSelf;

        /// <summary>Builds the menu under the given canvas, hidden.</summary>
        public void Build(Canvas canvas)
        {
            if (_panel != null)
            {
                return;
            }

            _panel = GreyboxUi.CreateFullScreenPanel("PauseMenu", canvas.transform, PanelColour);

            GreyboxUi.CreateLabel(_panel.transform, "PAUSED", HeadlineSize, new Vector2(0f, 140f));

            // Both controls state their size rather than leaning on the default, because the
            // number is the point: at the 24 this screen inherited, RESUME and QUIT TO HUB drew at
            // about 9.5 dp. The buttons grew with the words — 84 units tall left no margin around a
            // 36-unit line once the label box filled the whole control.
            _resume = GreyboxUi.CreateButton(
                _panel.transform, "RESUME", new Vector2(0f, 24f), ButtonSize, ResumeColour, BodySize);
            _resume.onClick.AddListener(() => Resumed?.Invoke());

            var quit = GreyboxUi.CreateButton(
                _panel.transform, "QUIT TO HUB", new Vector2(0f, -88f), ButtonSize, QuitColour, BodySize);
            quit.onClick.AddListener(() => Quit?.Invoke());

            // Hidden before the keeper is attached. Adding it to a live object would fire OnEnable
            // and select Resume immediately, leaving a hidden control focused for the whole match —
            // which on a pad means the bomb button also presses it.
            _panel.SetActive(false);

            var keeper = _panel.AddComponent<UiFocusKeeper>();
            keeper.Fallback = _resume.gameObject;
        }

        /// <summary>Shows the menu and puts focus on carrying on.</summary>
        public void Show()
        {
            if (_panel == null)
            {
                return;
            }

            _panel.SetActive(true);

            // Resume is focused, not quit. The safe option should be the one a blind press lands on.
            UiFocus.Select(_resume != null ? _resume.gameObject : null);
        }

        /// <summary>Hides the menu and gives up selection.</summary>
        public void Hide()
        {
            _panel?.SetActive(false);
            UiFocus.Clear();
        }
    }
}
