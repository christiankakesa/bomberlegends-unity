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

        [SerializeField]
        [Tooltip("Wipes downward as the skill recharges. Optional.")]
        private RectTransform? _cooldownOverlay;

        private RectTransform _rect = null!;
        private Camera? _eventCamera;
        private Vector2 _origin;
        private Vector2 _pressed;
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
            IntentButtons action,
            RectTransform? aimIndicator = null,
            RectTransform? cancelZone = null,
            RectTransform? cooldownOverlay = null)
        {
            _action = action;
            _aimIndicator = aimIndicator;
            _cancelZone = cancelZone;
            _cooldownOverlay = cooldownOverlay;

            // A cast latched by a previous life must not survive into this one.
            _castLatched = false;
            _latchedAim = Vector2.zero;
            IsAiming = false;

            ShowCancelZone(false);
            ShowAimIndicator(false);
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
            ShowCancelZone(false);
            ShowAimIndicator(false);
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

        /// <summary>
        /// Shows whether the skill can be used, and how far off it is if not.
        /// </summary>
        /// <remarks>
        /// A button that looks identical whether or not it will do anything teaches players to stop
        /// pressing it. One playtester deliberately hoarded both skills for a whole run because
        /// nothing on screen said they would come back.
        /// </remarks>
        /// <param name="ready">Whether a charge is available now.</param>
        /// <param name="recharge">How far through the recharge it is, from zero to one.</param>
        public void SetReadiness(bool ready, float recharge)
        {
            if (_cooldownOverlay != null)
            {
                // Wipes downward as it fills, so the covered part is what is still to wait.
                var remaining = Mathf.Clamp01(1f - recharge);

                _cooldownOverlay.gameObject.SetActive(!ready && remaining > 0.001f);
                _cooldownOverlay.localScale = new Vector3(1f, remaining, 1f);
            }

            var image = GetComponent<UnityEngine.UI.Image>();

            if (image != null)
            {
                var colour = image.color;
                colour.a = ready ? 0.85f : 0.4f;
                image.color = colour;
            }
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            IsAiming = true;
            _eventCamera = eventData.pressEventCamera;

            // Two reference points, because two different questions get asked on release. The aim
            // is measured from the button, so the knob tracks the thumb the way a stick does and the
            // drag direction is unambiguous. Whether it was a tap at all is measured from where the
            // finger landed — a press that does not travel is a tap wherever on the button it fell.
            // Measuring both from the centre made a still press anywhere outside a 26-pixel disc in
            // the middle of a 200-pixel button fire as an aimed shot towards where the thumb landed.
            _origin = RectTransformUtility.WorldToScreenPoint(_eventCamera, Rect.position);
            _pressed = eventData.position;
            _current = eventData.position;

            // The cancel zone has to exist from the first instant — a drag straight into it must
            // work. The arrow does not: showing it on press and hiding it a frame later, on a tap
            // that never leaves the tap threshold, reads as a flicker rather than as feedback. It
            // turns on only once UpdateIndicator decides a drag is actually underway.
            ShowCancelZone(true);
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

            ShowCancelZone(false);
            ShowAimIndicator(false);

            if (cancelled)
            {
                return;
            }

            // A tap fires with no aim; the simulation then uses whichever way the player is facing.
            var travelled = (_current - _pressed).magnitude >= _tapThresholdPixels;

            _latchedAim = travelled
                ? (_current - _origin).normalized
                : Vector2.zero;

            _castLatched = true;
        }

        private void OnDisable()
        {
            IsAiming = false;
            _castLatched = false;
            _latchedAim = Vector2.zero;
            ShowCancelZone(false);
            ShowAimIndicator(false);
        }

        private Vector2 Clamped() => Vector2.ClampMagnitude(_current - _origin, _radiusPixels);

        private bool IsOverCancelZone(Vector2 screenPosition) =>
            _cancelZone != null && _cancelZone.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(_cancelZone, screenPosition, _eventCamera);

        private void ShowCancelZone(bool visible)
        {
            if (_cancelZone != null)
            {
                // The way out only exists while there is something to back out of.
                _cancelZone.gameObject.SetActive(visible);
            }
        }

        private void ShowAimIndicator(bool visible)
        {
            if (_aimIndicator != null)
            {
                _aimIndicator.gameObject.SetActive(visible);
            }
        }

        private void UpdateIndicator()
        {
            if (_aimIndicator == null)
            {
                return;
            }

            // The same threshold the release decision uses. A wobble a real thumb cannot avoid
            // must not flash the arrow on, only for the release a moment later to prove it was
            // never an aim at all.
            if ((_current - _pressed).magnitude < _tapThresholdPixels)
            {
                ShowAimIndicator(false);
                return;
            }

            ShowAimIndicator(true);

            var delta = Clamped();

            _aimIndicator.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);

            // Grows with the drag, so the player can see how committed the throw is.
            _aimIndicator.localScale = new Vector3(1f, delta.magnitude / _radiusPixels, 1f);
        }
    }
}
