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
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(StartMatch);
            }
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
