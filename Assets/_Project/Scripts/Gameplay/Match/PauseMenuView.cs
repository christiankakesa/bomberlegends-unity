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

            GreyboxUi.CreateLabel(_panel.transform, "PAUSED", 40, new Vector2(0f, 140f));

            _resume = GreyboxUi.CreateButton(
                _panel.transform, "RESUME", new Vector2(0f, 20f), new Vector2(320f, 84f), ResumeColour);
            _resume.onClick.AddListener(() => Resumed?.Invoke());

            var quit = GreyboxUi.CreateButton(
                _panel.transform, "QUIT TO HUB", new Vector2(0f, -80f), new Vector2(320f, 84f), QuitColour);
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
