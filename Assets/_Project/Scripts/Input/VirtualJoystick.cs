using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BomberLegends.Input
{
    /// <summary>
    /// An on-screen thumbstick.
    /// </summary>
    /// <remarks>
    /// Reports displacement in screen space and nothing else; converting that into a grid direction
    /// belongs to <see cref="TouchInputSource"/>. The stick recentres on the first touch rather than
    /// sitting at a fixed point, because a thumb reaching for a fixed circle it cannot see is the
    /// most common cause of a missed first input.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField]
        [Tooltip("Moves with the thumb. Optional; the stick works without a visual.")]
        private RectTransform? _handle;

        [SerializeField, Min(1f)]
        [Tooltip("Screen pixels of travel that count as full displacement.")]
        private float _radiusPixels = 120f;

        [SerializeField]
        [Tooltip("Whether the stick recentres under the thumb when a touch begins.")]
        private bool _recentreOnPress = true;

        private RectTransform _rect = null!;
        private Vector2 _origin;

        /// <summary>Current displacement, from minus one to one on each axis.</summary>
        public Vector2 Value { get; private set; }

        /// <summary>Whether a thumb is currently on the stick.</summary>
        public bool IsPressed { get; private set; }

        private void Awake() => _rect = GetComponent<RectTransform>();

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;
            _origin = _recentreOnPress ? eventData.position : RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera, _rect.position);

            UpdateValue(eventData.position);
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData) => UpdateValue(eventData.position);

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
            Value = Vector2.zero;

            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }

        private void UpdateValue(Vector2 screenPosition)
        {
            var delta = screenPosition - _origin;
            var clamped = Vector2.ClampMagnitude(delta, _radiusPixels);

            Value = clamped / _radiusPixels;

            if (_handle != null)
            {
                _handle.anchoredPosition = clamped;
            }
        }
    }
}
