using UnityEngine;

namespace BomberLegends.Services
{
    /// <summary>
    /// Puts selection back whenever a screen loses it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Selection is lost more often than it looks: clicking empty space clears it, and so does
    /// hiding or destroying whatever was selected — which happens on every one of these screens as
    /// buttons appear and disappear. Once it is gone a pad is dead until a mouse rescues it.
    /// </para>
    /// <para>
    /// Deliberately a component rather than a global watcher, so it lives and dies with the screen
    /// it belongs to. Disabled with its panel, it stops running the moment play resumes, which is
    /// what keeps selection out of a match where Submit shares a button with Bomb.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class UiFocusKeeper : MonoBehaviour
    {
        /// <summary>Where selection returns to. Falls back to the first control found below this.</summary>
        public GameObject? Fallback { get; set; }

        private void OnEnable() => Restore();

        private void Update()
        {
            var current = UiFocus.Current;

            if (current != null && current.activeInHierarchy)
            {
                return;
            }

            Restore();
        }

        private void Restore()
        {
            if (Fallback != null && Fallback.activeInHierarchy)
            {
                UiFocus.Select(Fallback);
                return;
            }

            UiFocus.SelectFirstIn(gameObject);
        }
    }
}
