using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BomberLegends.Input
{
    /// <summary>
    /// An on-screen thumbstick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reports displacement in screen space and nothing else; converting that into a grid direction
    /// belongs to <see cref="TouchInputSource"/>. The stick recentres on the first touch rather than
    /// sitting at a fixed point, because a thumb reaching for a fixed circle it cannot see is the
    /// most common cause of a missed first input.
    /// </para>
    /// <para>
    /// <b>The area that listens and the circle that is drawn are two different things</b>, and that
    /// is the whole point. This component sits on the listening area — the bottom-left quarter of
    /// the screen — while the visual is the small circle that moves to meet the thumb. They used to
    /// be one 300-unit object, so recentring only worked for a press that had already hit the
    /// circle: press an inch away and nothing happened at all, which on a device reads as the game
    /// ignoring you rather than as a target missed by a thumb's width.
    /// </para>
    /// <para>
    /// The knob's travel is drawn as a fraction of its ring rather than in the screen pixels the
    /// value is measured in. A pixel and a canvas unit are not the same length on any two devices,
    /// so offsetting the knob by a pixel count would have it leave its ring on one phone and barely
    /// stir on another; scaling the ring by the value puts the knob against the rim exactly when
    /// the stick reads full, everywhere.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField]
        [Tooltip(
            "The circle that is drawn, which moves to meet the thumb. Optional: without one the " +
            "listening area is the circle, and a press outside it does nothing at all.")]
        private RectTransform? _visual;

        [SerializeField]
        [Tooltip("Moves with the thumb, inside the circle. Optional; the stick works without a visual.")]
        private RectTransform? _handle;

        [SerializeField, Min(1f)]
        [Tooltip("Screen pixels of travel that count as full displacement.")]
        private float _radiusPixels = 120f;

        [SerializeField]
        [Tooltip("Whether the stick moves to the thumb when a touch begins, anywhere in its area.")]
        private bool _recentreOnPress = true;

        private RectTransform _rect = null!;
        private Vector2 _origin;
        private Vector2 _home;
        private bool _knowsHome;

        /// <summary>Current displacement, from minus one to one on each axis.</summary>
        public Vector2 Value { get; private set; }

        /// <summary>Whether a thumb is currently on the stick.</summary>
        public bool IsPressed { get; private set; }

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            RememberHome();
        }

        /// <summary>Notes where the drawn circle rests, before anything has moved it.</summary>
        private void RememberHome()
        {
            if (_knowsHome || _visual == null)
            {
                return;
            }

            _home = _visual.anchoredPosition;
            _knowsHome = true;
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            RememberHome();

            IsPressed = true;

            if (_recentreOnPress)
            {
                // Wherever the thumb landed is the centre. The circle follows it, because a stick
                // that stayed in the corner while the numbers came from an inch away would be a
                // worse lie than having no stick drawn at all.
                _origin = eventData.position;
                MoveVisualTo(eventData.position, eventData.pressEventCamera);
            }
            else
            {
                _origin = RectTransformUtility.WorldToScreenPoint(
                    eventData.pressEventCamera, Anchor.position);
            }

            UpdateValue(eventData.position);
        }

        /// <summary>The circle, or this object when nothing separate is drawn.</summary>
        private RectTransform Anchor => _visual != null ? _visual : _rect;

        /// <summary>Puts the drawn circle under a screen point.</summary>
        private void MoveVisualTo(Vector2 screenPosition, Camera? camera)
        {
            if (_visual == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, screenPosition, camera, out var local))
            {
                _visual.anchoredPosition = local;
            }
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

            // Back to where it is drawn at rest, so the next thumb still has something to aim at
            // even though it no longer has to.
            if (_visual != null && _knowsHome)
            {
                _visual.anchoredPosition = _home;
            }
        }

        /// <summary>
        /// How far the stick is pushed, from minus one to one on each axis.
        /// </summary>
        /// <param name="delta">Travel from where the thumb landed, in screen pixels.</param>
        /// <param name="radiusPixels">The travel that counts as fully pushed.</param>
        public static Vector2 Displacement(Vector2 delta, float radiusPixels) =>
            radiusPixels <= 0f
                ? Vector2.zero
                : Vector2.ClampMagnitude(delta, radiusPixels) / radiusPixels;

        private void UpdateValue(Vector2 screenPosition)
        {
            Value = Displacement(screenPosition - _origin, _radiusPixels);

            if (_handle != null)
            {
                _handle.anchoredPosition = Value * KnobTravel();
            }
        }

        /// <summary>
        /// How far the knob may slide from the middle of its ring, in canvas units.
        /// </summary>
        /// <remarks>
        /// Measured off the two rects rather than authored, so the knob stays inside the ring when
        /// either is resized — and it is the ring's radius less the knob's own, or a knob at full
        /// deflection would hang half outside the circle it belongs to.
        /// </remarks>
        private float KnobTravel()
        {
            if (_handle == null)
            {
                return 0f;
            }

            var ring = Anchor.rect.width * 0.5f;
            var knob = _handle.rect.width * 0.5f;

            return Mathf.Max(0f, ring - knob);
        }
    }
}
