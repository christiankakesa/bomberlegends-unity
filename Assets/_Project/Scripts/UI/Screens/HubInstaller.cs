using BomberLegends.Services;
using BomberLegends.Services.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.UI.Screens
{
    /// <summary>
    /// Wires the hub scene. For now that is a single control that starts a match; the progression
    /// and upgrade screens arrive with the meta loop in T-033.
    /// </summary>
    public sealed class HubInstaller : SceneInstaller
    {
        [Header("Scene references")]
        [SerializeField]
        [Tooltip("Starts a match.")]
        private Button? _playButton;

        [SerializeField]
        [Tooltip("Leaves the game. Built at run time when nothing is assigned.")]
        private Button? _quitButton;

        private GameContext? _context;

        /// <inheritdoc />
        public override SceneId Scene => SceneId.Hub;

        /// <inheritdoc />
        protected override void OnInstall(GameContext context, ISceneTransitionPayload? payload)
        {
            _context = context;

            if (_playButton == null)
            {
                Debug.LogError("[Hub] No play button is assigned; a match cannot be started.");
                return;
            }

            _playButton.onClick.AddListener(StartMatch);

            // A browser tab cannot be quit by the page inside it: Application.Quit does nothing on
            // WebGL, so the control would be a button that visibly fails. The tab close button is
            // the platform's answer and it is already there.
#if !UNITY_WEBGL || UNITY_EDITOR
            _quitButton ??= CreateQuitButton();
            _quitButton?.onClick.AddListener(Quit);
#endif

            EnableNavigation();
        }

        /// <summary>
        /// Makes the hub reachable without a mouse.
        /// </summary>
        /// <remarks>
        /// The input module was always correct; nothing was ever selected. Unity's navigation moves
        /// from the current selection, so with none a d-pad does nothing and the screen looks
        /// broken to anyone not holding a mouse.
        /// </remarks>
        private void EnableNavigation()
        {
            if (_playButton == null)
            {
                return;
            }

            UiFocus.ApplyNavigationColours(_playButton, _playButton.image != null
                ? _playButton.image.color
                : Color.white);

            if (_quitButton != null)
            {
                UiFocus.ApplyNavigationColours(_quitButton, _quitButton.image != null
                    ? _quitButton.image.color
                    : Color.white);
            }

            var keeper = _playButton.transform.parent != null
                ? _playButton.transform.parent.gameObject.AddComponent<UiFocusKeeper>()
                : gameObject.AddComponent<UiFocusKeeper>();

            keeper.Fallback = _playButton.gameObject;

            UiFocus.Select(_playButton.gameObject);
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(StartMatch);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(Quit);
            }
        }

        private static void Quit() => ApplicationExit.Request();

        /// <summary>
        /// Builds a quit control beneath the play button.
        /// </summary>
        /// <remarks>
        /// Created at run time so the scene needs no regeneration to gain one. A game with no way
        /// out of it is a bug however small the button would have been.
        /// </remarks>
        private Button? CreateQuitButton()
        {
            if (_playButton == null)
            {
                return null;
            }

            var source = _playButton.GetComponent<RectTransform>();

            var host = new GameObject("QuitButton", typeof(RectTransform));
            host.transform.SetParent(_playButton.transform.parent, false);

            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            rect.anchoredPosition = source.anchoredPosition - new Vector2(0f, source.sizeDelta.y + 24f);

            var image = host.AddComponent<Image>();
            image.color = new Color(0.32f, 0.16f, 0.20f);

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(host.transform, false);

            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = label.AddComponent<Text>();
            text.text = "QUIT";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // 28 was about 11 dp on a phone: under the floor, and on the one control that gets a
            // player out of the game.
            text.fontSize = TextLegibility.MinimumBodySize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            return host.AddComponent<Button>();
        }

        private async void StartMatch()
        {
            if (_context == null)
            {
                return;
            }

            await _context.Scenes.TransitionToAsync(SceneId.Match);
        }
    }
}
