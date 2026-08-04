using System.Threading;
using UnityEngine;

namespace BomberLegends.Services.Scenes
{
    /// <summary>Covers the screen while scenes are swapped.</summary>
    public interface ILoadingScreen
    {
        /// <summary>Whether the screen is currently covering the view.</summary>
        bool IsVisible { get; }

        /// <summary>Fades the screen in and completes once it fully covers the view.</summary>
        Awaitable ShowAsync(CancellationToken cancellationToken = default);

        /// <summary>Fades the screen out and completes once it is fully transparent.</summary>
        Awaitable HideAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A full-screen fade driven by a <see cref="CanvasGroup"/>.
    /// </summary>
    /// <remarks>
    /// Lives on the persistent bootstrap scene so it survives every scene swap — a loading screen
    /// inside the scene being unloaded would disappear at the exact moment it is needed.
    /// Timing uses unscaled time so a paused or time-scaled game still transitions.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LoadingScreenView : MonoBehaviour, ILoadingScreen
    {
        [SerializeField, Min(0f)]
        [Tooltip("Seconds to fade to fully opaque.")]
        private float _fadeInSeconds = 0.15f;

        [SerializeField, Min(0f)]
        [Tooltip("Seconds to fade back to fully transparent.")]
        private float _fadeOutSeconds = 0.25f;

        private CanvasGroup _canvasGroup = null!;

        /// <inheritdoc />
        public bool IsVisible => _canvasGroup.alpha > 0f;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetAlpha(0f);
        }

        /// <inheritdoc />
        public async Awaitable ShowAsync(CancellationToken cancellationToken = default) =>
            await FadeToAsync(1f, _fadeInSeconds, cancellationToken);

        /// <inheritdoc />
        public async Awaitable HideAsync(CancellationToken cancellationToken = default) =>
            await FadeToAsync(0f, _fadeOutSeconds, cancellationToken);

        private async Awaitable FadeToAsync(float target, float duration, CancellationToken cancellationToken)
        {
            var start = _canvasGroup.alpha;

            if (duration <= 0f || Mathf.Approximately(start, target))
            {
                SetAlpha(target);
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            SetAlpha(target);
        }

        private void SetAlpha(float alpha)
        {
            _canvasGroup.alpha = alpha;

            // Only swallow input while something is actually covering the view.
            _canvasGroup.blocksRaycasts = alpha > 0f;
            _canvasGroup.interactable = alpha > 0f;
        }
    }
}
