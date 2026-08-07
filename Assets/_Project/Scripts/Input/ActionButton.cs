using BomberLegends.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberLegends.Input
{
    /// <summary>
    /// An on-screen action button that reports whether it is currently held.
    /// </summary>
    /// <remarks>
    /// Reports held state rather than firing a click event, because the simulation samples intent
    /// once per tick and a click delivered between ticks would be lost. Which tick the press lands
    /// on is decided by the simulation, not by the UI event queue.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        [Tooltip("Which action this button requests.")]
        private IntentButtons _action = IntentButtons.Bomb;

        /// <summary>The action this button requests.</summary>
        public IntentButtons Action => _action;

        /// <summary>Whether a finger is currently on it.</summary>
        public bool IsHeld { get; private set; }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData) => IsHeld = true;

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData) => IsHeld = false;

        private void OnDisable() => IsHeld = false;
    }
}
