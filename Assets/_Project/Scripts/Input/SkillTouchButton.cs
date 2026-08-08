using BomberLegends.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberLegends.Input
{
    /// <summary>
    /// A skill button that doubles as its own aiming stick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The control MOBAs settled on, and for a reason worth stating: with three skills, a single
    /// shared aim stick cannot know which one you meant, so aiming and choosing would need two
    /// gestures. Making each button its own stick collapses them into one — press it, drag to aim,
    /// release to fire.
    /// </para>
    /// <para>
    /// A press that does not travel is a <b>tap cast</b>: it fires with no aim at all, and the
    /// simulation falls back to the direction the player is already travelling. That fast path
    /// matters more than it looks, because most casts do not need precision and demanding a drag
    /// for all of them would make the game feel slow.
    /// </para>
    /// <para>
    /// Nothing is reported while the finger is down. The cast is latched on release and held until
    /// the simulation reads it, because ticks run at 30 Hz and a press that resolved between two of
    /// them would simply be lost.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SkillTouchButton : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField]
        [Tooltip("Which loadout slot this button casts.")]
        private IntentButtons _action = IntentButtons.Skill1;

        [SerializeField, Min(1f)]
        [Tooltip("Screen pixels of drag that count as a full-strength aim.")]
        private float _radiusPixels = 140f;

        [SerializeField, Min(0f)]
        [Tooltip("Drag below this many pixels is treated as a tap, firing with no aim.")]
        private float _tapThresholdPixels = 26f;

        [SerializeField]
        [Tooltip("Shown while aiming, rotated towards the drag. Optional.")]
        private RectTransform? _aimIndicator;

        [SerializeField]
        [Tooltip("Releasing inside this area abandons the cast. Optional but strongly advised.")]
        private RectTransform? _cancelZone;

        private RectTransform _rect = null!;
        private Camera? _eventCamera;
        private Vector2 _origin;
        private Vector2 _current;
        private bool _castLatched;
        private Vector2 _latchedAim;

        /// <summary>Which loadout slot this button casts.</summary>
        public IntentButtons Action => _action;

        /// <summary>
        /// Configures a button built at run time.
        /// </summary>
        /// <remarks>
        /// The greybox interface is created in code rather than authored, so these have to be
        /// settable without an Inspector. Serialized fields keep working for anything placed in a
        /// scene later; this only fills them in.
        /// </remarks>
        public void Initialise(
            IntentButtons action, RectTransform? aimIndicator = null, RectTransform? cancelZone = null)
        {
            _action = action;
            _aimIndicator = aimIndicator;
            _cancelZone = cancelZone;

            // A cast latched by a previous life must not survive into this one.
            _castLatched = false;
            _latchedAim = Vector2.zero;
            IsAiming = false;

            ShowIndicator(false);
        }

        /// <summary>Whether a finger is currently down on it.</summary>
        public bool IsAiming { get; private set; }

        /// <summary>
        /// The aim being drawn right now, from minus one to one on each axis in screen space.
        /// </summary>
        public Vector2 CurrentAim => IsAiming ? Clamped() / _radiusPixels : Vector2.zero;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            ShowIndicator(false);
        }

        private RectTransform Rect => _rect != null ? _rect : _rect = GetComponent<RectTransform>();

        /// <summary>
        /// Takes the pending cast, if there is one.
        /// </summary>
        /// <param name="aim">
        /// Screen-space aim, or zero for a tap cast where the simulation should use the direction
        /// of travel.
        /// </param>
        /// <returns>Whether a cast was waiting.</returns>
        public bool ConsumeCast(out Vector2 aim)
        {
            aim = _latchedAim;

            if (!_castLatched)
            {
                return false;
            }

            _castLatched = false;
            _latchedAim = Vector2.zero;
            return true;
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            IsAiming = true;
            _eventCamera = eventData.pressEventCamera;

            // Measured from the button rather than from the finger, so the knob tracks the thumb
            // the way a stick does and the drag direction is unambiguous.
            _origin = RectTransformUtility.WorldToScreenPoint(_eventCamera, Rect.position);
            _current = eventData.position;

            ShowIndicator(true);
            UpdateIndicator();
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            _current = eventData.position;
            UpdateIndicator();
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            _current = eventData.position;
            IsAiming = false;

            // Tested before anything is hidden. Hiding first would deactivate the very zone being
            // asked about, so no release could ever land in it and cancelling would never work.
            var cancelled = IsOverCancelZone(_current);

            ShowIndicator(false);

            if (cancelled)
            {
                return;
            }

            var delta = _current - _origin;

            // A tap fires with no aim; the simulation then uses whichever way the player is facing.
            _latchedAim = delta.magnitude < _tapThresholdPixels
                ? Vector2.zero
                : delta.normalized;

            _castLatched = true;
        }

        private void OnDisable()
        {
            IsAiming = false;
            _castLatched = false;
            _latchedAim = Vector2.zero;
            ShowIndicator(false);
        }

        private Vector2 Clamped() => Vector2.ClampMagnitude(_current - _origin, _radiusPixels);

        private bool IsOverCancelZone(Vector2 screenPosition) =>
            _cancelZone != null && _cancelZone.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(_cancelZone, screenPosition, _eventCamera);

        private void ShowIndicator(bool visible)
        {
            if (_aimIndicator != null)
            {
                _aimIndicator.gameObject.SetActive(visible);
            }

            if (_cancelZone != null)
            {
                // The way out only exists while there is something to back out of.
                _cancelZone.gameObject.SetActive(visible);
            }
        }

        private void UpdateIndicator()
        {
            if (_aimIndicator == null)
            {
                return;
            }

            var delta = Clamped();

            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                _aimIndicator.localScale = new Vector3(1f, 0f, 1f);
                return;
            }

            _aimIndicator.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);

            // Grows with the drag, so the player can see how committed the throw is.
            _aimIndicator.localScale = new Vector3(1f, delta.magnitude / _radiusPixels, 1f);
        }
    }
}
