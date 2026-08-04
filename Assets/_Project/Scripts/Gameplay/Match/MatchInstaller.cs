using BomberLegends.Services;
using BomberLegends.Services.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Match
{
    /// <summary>
    /// Wires the match scene. At this milestone it only provides a way back to the hub, so the
    /// full scene flow can be exercised end to end; the board, simulation and HUD arrive in
    /// Milestone 1.
    /// </summary>
    public sealed class MatchInstaller : SceneInstaller
    {
        [Header("Scene references")]
        [SerializeField]
        [Tooltip("Abandons the match and returns to the hub.")]
        private Button? _quitButton;

        private GameContext? _context;

        /// <inheritdoc />
        public override SceneId Scene => SceneId.Match;

        /// <inheritdoc />
        protected override void OnInstall(GameContext context, ISceneTransitionPayload? payload)
        {
            _context = context;

            if (_quitButton == null)
            {
                Debug.LogError("[Match] No quit button is assigned; the hub cannot be reached.");
                return;
            }

            _quitButton.onClick.AddListener(ReturnToHub);
        }

        private void OnDestroy()
        {
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(ReturnToHub);
            }
        }

        private async void ReturnToHub()
        {
            if (_context == null)
            {
                return;
            }

            await _context.Scenes.TransitionToAsync(SceneId.Hub);
        }
    }
}
