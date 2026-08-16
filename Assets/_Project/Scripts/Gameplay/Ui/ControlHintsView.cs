using BomberLegends.Gameplay.Ui;
using BomberLegends.Input;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Events;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Ui
{
    /// <summary>
    /// Teaches the controls by showing them until they are used, then getting out of the way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two of four playtesters never discovered that Space places a bomb. In a game built on
    /// bombing a maze, a player who does not know they can bomb is simply trapped — one of them
    /// described it as a labyrinth with no way out, and another concluded the game was broken.
    /// </para>
    /// <para>
    /// The mobile build did not have this problem, and the reason is worth copying rather than
    /// admiring: it has a button on screen with BOMB written on it. This does the same thing for a
    /// keyboard and a pad.
    /// </para>
    /// <para>
    /// Each hint retires the moment the player performs that action, so the display teaches and then
    /// disappears rather than becoming furniture. Nothing has to be read twice, and a player who
    /// already knows sees it for one bomb placement.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ControlHintsView : MonoBehaviour
    {
        private static readonly Color PanelColour = new Color(0.04f, 0.05f, 0.10f, 0.72f);
        private static readonly Color GoalColour = new Color(1f, 0.85f, 0.35f);

        private ControlSchemeTracker? _devices;
        private GameObject? _panel;
        private Text? _goal;
        private Text? _hints;

        private bool _bombed;
        private bool _dashed;
        private bool _shot;
        private bool _cleared;
        private ControlScheme _shownFor = (ControlScheme)255;
        private string _lastText = string.Empty;

        /// <summary>Builds the panel under the given canvas.</summary>
        public void Begin(Canvas canvas, ControlSchemeTracker devices)
        {
            _devices = devices;

            if (_panel != null)
            {
                return;
            }

            _panel = new GameObject("ControlHints", typeof(RectTransform));
            _panel.transform.SetParent(canvas.transform, false);

            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(900f, 130f);

            _panel.AddComponent<Image>().color = PanelColour;

            _goal = GreyboxUi.CreateLabel(
                _panel.transform, string.Empty, 26, new Vector2(0f, 36f), new Vector2(860f, 44f));
            _goal.color = GoalColour;

            _hints = GreyboxUi.CreateLabel(
                _panel.transform, string.Empty, 22, new Vector2(0f, -22f), new Vector2(860f, 60f));
        }

        /// <summary>Retires whichever hints the player has just proved they no longer need.</summary>
        public void Consume(GameSimulation simulation)
        {
            var events = simulation.Events;

            for (var i = 0; i < events.Count; i++)
            {
                switch (events[i].Type)
                {
                    case SimEventType.BombPlaced:
                        _bombed = true;
                        break;

                    case SimEventType.DashStarted:
                        _dashed = true;
                        break;

                    case SimEventType.ProjectileFired:
                        _shot = true;
                        break;

                    // Clearing a sector is the proof that the goal landed. Nothing needs to say it
                    // again for the rest of the run.
                    case SimEventType.ArenaCleared:
                        _cleared = true;
                        break;
                }
            }
        }

        /// <summary>Redraws for whichever device is in charge.</summary>
        public void Render()
        {
            if (_panel == null || _goal == null || _hints == null || _devices == null)
            {
                return;
            }

            var scheme = _devices.Current;

            // Exactly the inverse of the on-screen controls, reusing their rule rather than writing
            // a second answer to the same question. The touch build already labels every control,
            // which is why it was the one build nobody got lost in.
            //
            // The rule matters most before anything has been touched: the tracker reports
            // KeyboardMouse by default, so a naive check would flash [SPACE] BOMB at someone
            // holding a phone.
            var touchControlsShowing = TouchControlVisibility.ShouldShow(
                _devices.HasBeenUsed, scheme, HasPointerHardware);

            if (touchControlsShowing)
            {
                Show(false);
                return;
            }

            var text = BuildHints(scheme);
            var done = _cleared && text.Length == 0;

            Show(!done);

            if (done)
            {
                return;
            }

            _goal.text = _cleared ? string.Empty : "DESTROY EVERY SENTINEL TO CLEAR THE SECTOR";

            if (text != _lastText || scheme != _shownFor)
            {
                _lastText = text;
                _shownFor = scheme;
                _hints.text = text;
            }
        }

        /// <summary>The bindings the player has not yet used, most important first.</summary>
        private string BuildHints(ControlScheme scheme)
        {
            var pad = scheme == ControlScheme.Gamepad;

            // Bomb leads, always. It is the verb the whole game is built on and the one that was
            // actually missed; a player who never finds it cannot find anything else either.
            var bomb = _bombed ? null : pad ? "(A) BOMB" : "[SPACE] BOMB";
            var move = _bombed ? null : pad ? "(L-STICK) MOVE" : "[WASD] MOVE";
            var dash = _dashed ? null : pad ? "(B) DASH" : "[SHIFT] DASH";
            var shot = _shot ? null : pad ? "(X) SHOOT" : "[Q / LEFT-CLICK] SHOOT";

            var parts = new System.Collections.Generic.List<string>(4);

            if (move != null)
            {
                parts.Add(move);
            }

            if (bomb != null)
            {
                parts.Add(bomb);
            }

            if (dash != null)
            {
                parts.Add(dash);
            }

            if (shot != null)
            {
                parts.Add(shot);
            }

            return string.Join("     ", parts);
        }

        private static bool HasPointerHardware =>
            UnityEngine.InputSystem.Mouse.current != null ||
            UnityEngine.InputSystem.Keyboard.current != null;

        private void Show(bool visible)
        {
            if (_panel != null && _panel.activeSelf != visible)
            {
                _panel.SetActive(visible);
            }
        }
    }
}
