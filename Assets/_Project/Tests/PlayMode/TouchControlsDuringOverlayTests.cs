using System.Collections;
using System.Collections.Generic;
using BomberLegends.Gameplay.Run;
using BomberLegends.Gameplay.Ui;
using BomberLegends.Input;
using BomberLegends.Simulation.Items;
using BomberLegends.Simulation.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BomberLegends.Tests.PlayMode
{
    /// <summary>
    /// Keeps the on-screen controls out of the way of whatever is covering the match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Found on device, on the build that fixed the choice screen's text: the SHOT button was
    /// sitting on top of the right-hand choice card. The cluster is anchored to the bomb button in
    /// the bottom-right corner, which is exactly where the third card is drawn, and nothing hid it
    /// while the overlay was up.
    /// </para>
    /// <para>
    /// It was not only ugly. The skill cluster is built after the overlay, so it is a later sibling
    /// and draws on top — and being a raycast target, it also took the taps meant for the card
    /// underneath. The card could not reliably be chosen on touch, which is the one input the whole
    /// screen was rebuilt for.
    /// </para>
    /// </remarks>
    public sealed class TouchControlsDuringOverlayTests
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

        [UnityTest]
        public IEnumerator TheControlsStandDownWhileTheMatchIsCoveredAndComeBackAfter()
        {
            _root = PhoneCanvas.Build(PhoneCanvas.GalaxyS21Ultra, out _);

            var controls = new[] { Control("Stick"), Control("Bomb") };
            var covered = false;

            var devices = new ControlSchemeTracker();
            devices.ForceScheme(ControlScheme.Touch);

            var visibility = _root.AddComponent<TouchControlVisibility>();
            visibility.Covered = () => covered;
            visibility.Begin(devices, controls[0], controls[1]);

            Assert.That(controls[0].activeSelf, Is.True,
                "a touch build opened with its controls already hidden");

            covered = true;
            yield return null;

            Assert.That(controls[0].activeSelf, Is.False, "the stick stayed up over the overlay");
            Assert.That(controls[1].activeSelf, Is.False, "the bomb button stayed up over the overlay");

            // The half that is easy to get wrong. A control hidden and never given back is a match
            // that cannot be played after the first item choice.
            covered = false;
            yield return null;

            Assert.That(controls[0].activeSelf, Is.True, "the stick never came back");
            Assert.That(controls[1].activeSelf, Is.True, "the bomb button never came back");
        }

        [UnityTest]
        public IEnumerator NoTouchControlSitsOnTheChoiceCards()
        {
            _root = PhoneCanvas.Build(PhoneCanvas.GalaxyS21Ultra, out _);
            var canvas = _root.GetComponent<Canvas>();

            // Built in the order the match builds them, because the order is half the defect: the
            // overlay first, the cluster after it, which is what puts the cluster on top.
            var overlay = _root.AddComponent<RunOverlayView>();
            overlay.Build(canvas);

            var anchor = BuildBombButtonAnchor(canvas);
            var loadout = SkillLoadout.Of(
                new SkillSlot { Id = SkillId.Dash, MaxCharges = 1, Charges = 1 },
                new SkillSlot { Id = SkillId.Skillshot, MaxCharges = 1, Charges = 1 });

            var skills = TouchControlsBuilder.Build(anchor, loadout);

            var touchControls = new List<GameObject> { anchor.gameObject };
            for (var i = 0; i < skills.Length; i++)
            {
                touchControls.Add(skills[i].gameObject);
            }

            var devices = new ControlSchemeTracker();
            devices.ForceScheme(ControlScheme.Touch);

            var visibility = _root.AddComponent<TouchControlVisibility>();
            visibility.Covered = () => overlay.IsShowing;
            visibility.Begin(devices, touchControls.ToArray());

            overlay.ShowChoices(Offers(), arenaNumber: 3);
            yield return null;

            foreach (var control in touchControls)
            {
                Assert.That(control.activeInHierarchy, Is.False,
                    $"{control.name} is still up while the choice screen is showing");
            }

            // Stated as overlap rather than as "everything is hidden", so that moving the cluster
            // off the cards would satisfy it too. What must not happen is a live control sharing
            // screen with a card, however that comes about.
            foreach (var control in touchControls)
            {
                if (!control.activeInHierarchy)
                {
                    continue;
                }

                var controlRect = CanvasRectOf(control.GetComponent<RectTransform>());

                foreach (var button in overlay.GetComponentsInChildren<Button>(includeInactive: false))
                {
                    Assert.That(controlRect.Overlaps(CanvasRectOf(button.GetComponent<RectTransform>())),
                        Is.False,
                        $"{control.name} covers a control on the choice screen, so a tap meant " +
                        "for the card lands on it instead");
                }
            }

            // And back, or the second arena is unplayable.
            overlay.Hide();
            yield return null;

            foreach (var control in touchControls)
            {
                Assert.That(control.activeInHierarchy, Is.True,
                    $"{control.name} never came back after the choice was made");
            }
        }

        [UnityTest]
        public IEnumerator TheOverlayDrawsAboveAnythingElseOnTheCanvas()
        {
            _root = PhoneCanvas.Build(PhoneCanvas.GalaxyS21Ultra, out _);
            var canvas = _root.GetComponent<Canvas>();

            var overlay = _root.AddComponent<RunOverlayView>();
            overlay.Build(canvas);

            // Created after the overlay, exactly as the match creates them, so they are later
            // siblings and the overlay starts underneath. Two more stand in for whatever else ends
            // up on this canvas next; the point of the assertion is that it does not matter what.
            var anchor = BuildBombButtonAnchor(canvas);
            TouchControlsBuilder.Build(anchor, SkillLoadout.Of(
                new SkillSlot { Id = SkillId.Dash, MaxCharges = 1, Charges = 1 }));

            Control("SomethingAddedLater");
            Control("SomethingAddedLaterStill");

            var panel = canvas.transform.Find("RunOverlay");
            Assert.That(panel, Is.Not.Null, "the overlay no longer builds a panel named RunOverlay");

            overlay.ShowChoices(Offers(), arenaNumber: 3);
            yield return null;

            // Stated against every sibling rather than against a known index, because the defect
            // was never about a particular number: it was about the overlay being built before the
            // things that end up covering it.
            for (var i = 0; i < canvas.transform.childCount; i++)
            {
                var sibling = canvas.transform.GetChild(i);

                if (sibling == panel)
                {
                    continue;
                }

                Assert.That(panel!.GetSiblingIndex(), Is.GreaterThan(sibling.GetSiblingIndex()),
                    $"{sibling.name} draws over the choice screen, so it also takes the taps " +
                    "meant for whatever is underneath it");
            }
        }

        private GameObject Control(string name)
        {
            var control = new GameObject(name, typeof(RectTransform), typeof(Image));
            control.transform.SetParent(_root!.transform, false);
            return control;
        }

        /// <summary>The bomb button as the match scene authors it, since the cluster is laid out
        /// from its size and corner.</summary>
        private static RectTransform BuildBombButtonAnchor(Canvas canvas)
        {
            var host = new GameObject("BombButton", typeof(RectTransform), typeof(Image));
            host.transform.SetParent(canvas.transform, false);

            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 240f);
            rect.anchoredPosition = new Vector2(-240f, 240f);

            return rect;
        }

        private Rect CanvasRectOf(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            var min = _root!.transform.InverseTransformPoint(corners[0]);
            var max = _root.transform.InverseTransformPoint(corners[2]);

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static ItemId[] Offers() => new[]
        {
            ItemId.Overcharge, ItemId.Momentum, ItemId.KineticCore,
        };
    }
}
