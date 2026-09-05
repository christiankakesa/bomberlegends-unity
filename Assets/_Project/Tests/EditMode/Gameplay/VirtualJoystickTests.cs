using BomberLegends.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers the on-screen stick: where a press counts from, and how far it reads.
    /// </summary>
    /// <remarks>
    /// The control that decides whether the player moves at all, and the one whose failure is
    /// hardest to see from a desk — a thumb that lands beside the drawn circle looks to the player
    /// like the game ignoring them, not like a target missed by half an inch.
    /// </remarks>
    public sealed class VirtualJoystickTests
    {
        private GameObject? _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        // ---------- the rule ----------

        [Test]
        public void AThumbAtRestReadsAsNoInput()
        {
            Assert.That(VirtualJoystick.Displacement(Vector2.zero, 120f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void FullTravelReadsAsFullyPushed()
        {
            var value = VirtualJoystick.Displacement(new Vector2(120f, 0f), 120f);

            Assert.That(value.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(value.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void PushingPastTheRingNeverReadsAboveFull()
        {
            // A thumb can travel the width of the screen. The stick must not report a speed the
            // simulation has no idea what to do with.
            var value = VirtualJoystick.Displacement(new Vector2(0f, 900f), 120f);

            Assert.That(value.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(value.y, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void HalfTravelReadsAsHalf()
        {
            // Analogue, not a d-pad: walking slowly has to be expressible.
            var value = VirtualJoystick.Displacement(new Vector2(0f, -60f), 120f);

            Assert.That(value.y, Is.EqualTo(-0.5f).Within(0.001f));
        }

        [Test]
        public void ARingOfNothingIsNotADivisionByZero()
        {
            Assert.That(VirtualJoystick.Displacement(new Vector2(50f, 50f), 0f), Is.EqualTo(Vector2.zero));
        }

        // ---------- pressing anywhere ----------

        [Test]
        public void WhereverTheThumbLandsIsTheCentre()
        {
            // The bug this control had: the press point became the origin only when the press had
            // already hit the drawn circle. Anywhere else in the thumb zone moved nothing at all.
            var joystick = CreateJoystick();

            Press(joystick, new Vector2(1200f, 800f));

            Assert.That(joystick.IsPressed, Is.True, "a press anywhere in the area must take");
            Assert.That(joystick.Value, Is.EqualTo(Vector2.zero),
                "and it must read as centred, however far from the circle it landed");
        }

        [Test]
        public void TravelIsMeasuredFromThePressAndNotFromTheCircle()
        {
            var joystick = CreateJoystick();

            Press(joystick, new Vector2(1200f, 800f));
            Drag(joystick, new Vector2(1200f + 120f, 800f));

            Assert.That(joystick.Value.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(joystick.Value.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void TwoPressesFarApartReadTheSame()
        {
            // What "press anywhere" has to mean: the same gesture reports the same thing wherever
            // in the quarter it is made.
            var joystick = CreateJoystick();

            Press(joystick, new Vector2(100f, 100f));
            Drag(joystick, new Vector2(100f, 100f + 60f));
            var near = joystick.Value;
            Release(joystick, new Vector2(100f, 160f));

            Press(joystick, new Vector2(900f, 500f));
            Drag(joystick, new Vector2(900f, 500f + 60f));
            var far = joystick.Value;

            Assert.That(far.x, Is.EqualTo(near.x).Within(0.001f));
            Assert.That(far.y, Is.EqualTo(near.y).Within(0.001f));
        }

        [Test]
        public void LettingGoCentresTheStick()
        {
            var joystick = CreateJoystick();

            Press(joystick, new Vector2(400f, 300f));
            Drag(joystick, new Vector2(600f, 300f));
            Assume.That(joystick.Value.x, Is.GreaterThan(0.5f));

            Release(joystick, new Vector2(600f, 300f));

            Assert.That(joystick.IsPressed, Is.False);
            Assert.That(joystick.Value, Is.EqualTo(Vector2.zero),
                "a released stick must stop asking for movement");
        }

        private VirtualJoystick CreateJoystick()
        {
            _root ??= new GameObject("Canvas", typeof(RectTransform));

            var zone = new GameObject("Joystick", typeof(RectTransform), typeof(VirtualJoystick));
            zone.transform.SetParent(_root.transform, false);

            var rect = zone.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(960f, 540f);

            return zone.GetComponent<VirtualJoystick>();
        }

        private static PointerEventData At(Vector2 position) =>
            new PointerEventData(EventSystem.current) { position = position };

        private static void Press(VirtualJoystick joystick, Vector2 at) =>
            joystick.OnPointerDown(At(at));

        private static void Drag(VirtualJoystick joystick, Vector2 to) => joystick.OnDrag(At(to));

        private static void Release(VirtualJoystick joystick, Vector2 at) =>
            joystick.OnPointerUp(At(at));
    }
}
