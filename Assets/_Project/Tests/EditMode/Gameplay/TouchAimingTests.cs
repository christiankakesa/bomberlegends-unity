using BomberLegends.Core;
using BomberLegends.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers the drag-to-aim skill buttons that make the hybrid playable on a touch screen.
    /// </summary>
    /// <remarks>
    /// Pointer events are delivered by hand rather than through an <c>EventSystem</c>. The gesture
    /// rules — what counts as a tap, what counts as an aim, what abandons the cast — are the whole
    /// control scheme, and they are far too easy to break silently to leave to on-device testing.
    /// </remarks>
    public sealed class TouchAimingTests
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

        private SkillTouchButton CreateButton(
            RectTransform? cancelZone = null, RectTransform? aimIndicator = null)
        {
            _root ??= new GameObject("TouchRoot", typeof(RectTransform));

            var host = new GameObject("SkillButton", typeof(RectTransform));
            host.transform.SetParent(_root.transform, false);

            var button = host.AddComponent<SkillTouchButton>();
            button.Initialise(IntentButtons.Skill2, aimIndicator: aimIndicator, cancelZone: cancelZone);

            return button;
        }

        private RectTransform CreateIndicator()
        {
            _root ??= new GameObject("TouchRoot", typeof(RectTransform));

            var indicator = new GameObject("AimIndicator", typeof(RectTransform)).GetComponent<RectTransform>();
            indicator.SetParent(_root.transform, false);

            return indicator;
        }

        private static PointerEventData At(Vector2 position) =>
            new PointerEventData(EventSystem.current) { position = position };

        private static void Press(SkillTouchButton button, Vector2 at) =>
            button.OnPointerDown(At(at));

        private static void DragTo(SkillTouchButton button, Vector2 to) => button.OnDrag(At(to));

        private static void Release(SkillTouchButton button, Vector2 at) =>
            button.OnPointerUp(At(at));

        // ---------- tap ----------

        [Test]
        public void ATapCastsWithNoAim()
        {
            // The fast path. Most casts do not need precision, and demanding a drag for all of them
            // would make the game feel slow; the simulation falls back to the direction of travel.
            var button = CreateButton();

            Press(button, Vector2.zero);
            Release(button, Vector2.zero);

            Assert.That(button.ConsumeCast(out var aim), Is.True);
            Assert.That(aim, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ATinyWobbleIsStillATap()
        {
            // A thumb never lands and leaves on exactly the same pixel.
            var button = CreateButton();

            Press(button, Vector2.zero);
            DragTo(button, new Vector2(6f, -4f));
            Release(button, new Vector2(6f, -4f));

            Assert.That(button.ConsumeCast(out var aim), Is.True);
            Assert.That(aim, Is.EqualTo(Vector2.zero), "a wobble must not be read as an aim");
        }

        // ---------- the arrow itself ----------

        [Test]
        public void AWobbleUnderTheThresholdNeverShowsTheArrow()
        {
            // Real thumbs cannot hold a pixel-perfect press, and the old code showed the arrow the
            // instant the finger touched down. A tap that wobbled a few pixels then flashed the
            // arrow on and off for a frame, reading as a glitch rather than as feedback.
            var indicator = CreateIndicator();
            var button = CreateButton(aimIndicator: indicator);

            Press(button, Vector2.zero);
            Assert.That(indicator.gameObject.activeSelf, Is.False,
                "the arrow must not appear on press alone");

            DragTo(button, new Vector2(6f, -4f));
            Assert.That(indicator.gameObject.activeSelf, Is.False,
                "a wobble under the tap threshold must not show the arrow");

            Release(button, new Vector2(6f, -4f));
            Assert.That(indicator.gameObject.activeSelf, Is.False, "and it must not stick on after release");
        }

        [Test]
        public void CrossingTheThresholdShowsTheArrowAndReleasingHidesIt()
        {
            var indicator = CreateIndicator();
            var button = CreateButton(aimIndicator: indicator);

            Press(button, Vector2.zero);
            DragTo(button, new Vector2(0f, 200f));
            Assert.That(indicator.gameObject.activeSelf, Is.True,
                "a real drag past the threshold must show the arrow");

            Release(button, new Vector2(0f, 200f));
            Assert.That(indicator.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ATapAwayFromTheCentreIsStillATap()
        {
            // The button is some two hundred screen pixels across on a phone and a thumb lands
            // wherever it lands. Whether a press was a tap is a question about how far the finger
            // travelled, not about how far from the middle it happened to come down — measured from
            // the centre, a still press anywhere outside a small disc fired as an aimed shot towards
            // the thumb, which is not a control anyone can learn.
            var button = CreateButton();

            Press(button, new Vector2(60f, 0f));
            Release(button, new Vector2(60f, 0f));

            Assert.That(button.ConsumeCast(out var aim), Is.True);
            Assert.That(aim, Is.EqualTo(Vector2.zero),
                "a press that did not move must fire with no aim wherever it landed");
        }

        [Test]
        public void ADragFromOffCentreStillAimsFromTheButton()
        {
            // The two measurements are deliberately different. A tap is judged by travel; an aim is
            // read from the button's centre, so the knob tracks the thumb like a stick and the
            // indicator on screen agrees with the shot that comes out.
            var button = CreateButton();

            Press(button, new Vector2(60f, 0f));
            DragTo(button, new Vector2(60f, 200f));
            Release(button, new Vector2(60f, 200f));

            var expected = new Vector2(60f, 200f).normalized;

            Assert.That(button.ConsumeCast(out var aim), Is.True);
            Assert.That(aim.x, Is.EqualTo(expected.x).Within(0.001f));
            Assert.That(aim.y, Is.EqualTo(expected.y).Within(0.001f));
        }

        // ---------- drag ----------

        [Test]
        public void ADragCastsTowardsWhereItWasPulled()
        {
            var button = CreateButton();

            Press(button, Vector2.zero);
            DragTo(button, new Vector2(0f, 200f));
            Release(button, new Vector2(0f, 200f));

            Assert.That(button.ConsumeCast(out var aim), Is.True);
            Assert.That(aim.y, Is.GreaterThan(0.9f));
            Assert.That(Mathf.Abs(aim.x), Is.LessThan(0.1f));
        }

        [Test]
        public void TheAimIsADirectionRatherThanADistance()
        {
            // How far the thumb travelled says nothing about how far the skill goes; only where.
            var near = CreateButton();
            Press(near, Vector2.zero);
            DragTo(near, new Vector2(60f, 0f));
            Release(near, new Vector2(60f, 0f));

            var far = CreateButton();
            Press(far, Vector2.zero);
            DragTo(far, new Vector2(600f, 0f));
            Release(far, new Vector2(600f, 0f));

            near.ConsumeCast(out var nearAim);
            far.ConsumeCast(out var farAim);

            Assert.That(nearAim.x, Is.EqualTo(farAim.x).Within(0.01f));
        }

        [Test]
        public void AimingIsReportedWhileTheThumbIsDown()
        {
            // The indicator has to be able to draw where the shot is going before it is fired.
            var button = CreateButton();

            Assert.That(button.IsAiming, Is.False);

            Press(button, Vector2.zero);
            DragTo(button, new Vector2(140f, 0f));

            Assert.That(button.IsAiming, Is.True);
            Assert.That(button.CurrentAim.x, Is.GreaterThan(0.5f));

            Release(button, new Vector2(140f, 0f));
            Assert.That(button.IsAiming, Is.False);
        }

        [Test]
        public void NothingIsCastWhileTheThumbIsStillDown()
        {
            // Firing on press would make every aim impossible: the skill would leave before the
            // player had drawn a direction.
            var button = CreateButton();

            Press(button, Vector2.zero);
            DragTo(button, new Vector2(0f, 200f));

            Assert.That(button.ConsumeCast(out _), Is.False);
        }

        // ---------- cancel ----------

        [Test]
        public void ReleasingOverTheCancelZoneAbandonsTheCast()
        {
            _root ??= new GameObject("TouchRoot", typeof(RectTransform));

            var zone = new GameObject("Cancel", typeof(RectTransform)).GetComponent<RectTransform>();
            zone.SetParent(_root.transform, false);
            zone.sizeDelta = new Vector2(300f, 200f);
            zone.position = new Vector3(0f, 400f, 0f);

            var button = CreateButton(zone);

            Press(button, Vector2.zero);
            DragTo(button, new Vector2(0f, 400f));
            Release(button, new Vector2(0f, 400f));

            Assert.That(button.ConsumeCast(out _), Is.False,
                "a cast dragged into the cancel zone must not fire");
        }

        // ---------- latching ----------

        [Test]
        public void ACastIsDeliveredExactlyOnce()
        {
            // The simulation triggers a skill on the press edge, so a cast reported for two ticks
            // would either fire twice or waste a charge.
            var button = CreateButton();

            Press(button, Vector2.zero);
            Release(button, Vector2.zero);

            Assert.That(button.ConsumeCast(out _), Is.True);
            Assert.That(button.ConsumeCast(out _), Is.False);
        }

        [Test]
        public void ACastWaitsUntilTheSimulationAsksForIt()
        {
            // Ticks run at 30 Hz while fingers do not. A press that resolved between two ticks
            // would simply be lost if the button only reported its state at that instant.
            var button = CreateButton();

            Press(button, Vector2.zero);
            Release(button, Vector2.zero);

            // Several frames pass with no tick.
            Assert.That(button.IsAiming, Is.False);
            Assert.That(button.ConsumeCast(out _), Is.True, "the cast must survive until it is read");
        }

        [Test]
        public void ReconfiguringDropsAnyPendingCast()
        {
            // Controls are rebuilt when a match starts. A cast latched by the previous life of a
            // button must not fire into the new one.
            var button = CreateButton();

            Press(button, Vector2.zero);
            Release(button, Vector2.zero);

            button.Initialise(IntentButtons.Skill1);

            Assert.That(button.ConsumeCast(out _), Is.False);
            Assert.That(button.Action, Is.EqualTo(IntentButtons.Skill1));
        }
    }
}
