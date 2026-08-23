using System.Collections;
using BomberLegends.Gameplay.Run;
using BomberLegends.Services;
using BomberLegends.Simulation.Items;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BomberLegends.Tests.PlayMode
{
    /// <summary>
    /// Holds the between-arena choice screen to a size a phone can actually read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written after the gate: on touch, 0 of 3 testers could describe their build and 3 of 3 picked
    /// at random, while pad testers reading the same cards on a monitor chose deliberately. The
    /// cards were not wrong, they were small — the description rendered at about 9 dp against a
    /// floor of roughly 14.
    /// </para>
    /// <para>
    /// The sizes in the view were picked from that conversion, so the conversion is what is
    /// asserted here. A future tweak that shrinks a font or widens a card fails with the number it
    /// broke, rather than being discovered by the next set of testers.
    /// </para>
    /// </remarks>
    public sealed class ChoiceCardLegibilityTests
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
        public IEnumerator EveryWordOnAChoiceCardIsBigEnoughToReadOnAPhone()
        {
            // A Galaxy S21 Ultra in landscape, the device this project is verified on: 3200 x 1440
            // at roughly 3.22 physical pixels per dp.
            var view = BuildOverlay(PhoneCanvas.GalaxyS21Ultra, out var scale);

            view.ShowChoices(Offers(), arenaNumber: 3);
            yield return null;

            foreach (var text in view.GetComponentsInChildren<Text>(includeInactive: false))
            {
                if (string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                var dp = PhoneCanvas.DpOf(text.fontSize, scale);

                Assert.That(dp, Is.GreaterThanOrEqualTo(TextLegibility.MinimumBodyDp),
                    $"\"{text.text}\" renders at {dp:F1} dp, under the " +
                    $"{TextLegibility.MinimumBodyDp} dp floor; this is the failure that made touch " +
                    "testers choose at random");
            }
        }

        [UnityTest]
        public IEnumerator TheCardsFitTheNarrowestPhoneThisShipsOn()
        {
            // The scaler trades width for height: a longer phone gains canvas width and loses
            // canvas height, so a 21:9 handset is the one that runs out of room vertically while a
            // 16:9 one is tightest across. Both are checked rather than argued about.
            var shapes = new[] { new Vector2(1920f, 1080f), new Vector2(2520f, 1080f) };

            foreach (var shape in shapes)
            {
                var view = BuildOverlay(shape, out _);
                var canvasRect = _root!.GetComponent<RectTransform>().rect;

                view.ShowChoices(Offers(), arenaNumber: 3);
                yield return null;

                foreach (var button in view.GetComponentsInChildren<Button>(includeInactive: false))
                {
                    var rect = button.GetComponent<RectTransform>();
                    var corners = new Vector3[4];
                    rect.GetLocalCorners(corners);

                    var centre = (Vector2)rect.localPosition;
                    var halfWidth = (corners[2].x - corners[0].x) * 0.5f;
                    var halfHeight = (corners[2].y - corners[0].y) * 0.5f;

                    Assert.That(Mathf.Abs(centre.x) + halfWidth,
                        Is.LessThanOrEqualTo(canvasRect.width * 0.5f),
                        $"a control runs off the side of a {shape.x}x{shape.y} screen");

                    Assert.That(Mathf.Abs(centre.y) + halfHeight,
                        Is.LessThanOrEqualTo(canvasRect.height * 0.5f),
                        $"a control runs off the top or bottom of a {shape.x}x{shape.y} screen");
                }

                TearDown();
            }
        }

        [UnityTest]
        public IEnumerator ACardSaysWhatPressingItWillDo()
        {
            // The gate's finding was not that the cards were wrong but that the moment did not read
            // as a decision at all. Taking and giving up are the same card in the same place, so the
            // word on it is the only thing that separates them.
            var view = BuildOverlay(new Vector2(1920f, 1080f), out _);

            view.ShowChoices(Offers(), arenaNumber: 3);
            yield return null;

            Assert.That(FindText(view, "TAKE"), Is.True, "a choice card never says what it does");
            Assert.That(FindText(view, "CHOOSE ONE"), Is.True, "the screen never asks for a choice");

            view.ShowDiscard(Offers(), ItemId.Overclock);
            yield return null;

            Assert.That(FindText(view, "GIVE UP"), Is.True,
                "a discard card still reads TAKE, so it invites the opposite of what it does");
        }

        private static bool FindText(RunOverlayView view, string wanted)
        {
            foreach (var text in view.GetComponentsInChildren<Text>(includeInactive: false))
            {
                if (text.text == wanted)
                {
                    return true;
                }
            }

            return false;
        }

        private static ItemId[] Offers() => new[]
        {
            ItemId.Overcharge, ItemId.Momentum, ItemId.KineticCore,
        };

        /// <summary>Builds the overlay on a canvas scaled the way the real scenes scale it.</summary>
        /// <remarks>
        /// The scaling itself lives in <see cref="PhoneCanvas"/>, shared with the audit of the
        /// other greybox screens. One copy of that arithmetic, because two copies of it is how the
        /// screens came to disagree about what a readable size was in the first place.
        /// </remarks>
        private RunOverlayView BuildOverlay(Vector2 resolution, out float scale)
        {
            _root = PhoneCanvas.Build(resolution, out scale);

            var view = _root.AddComponent<RunOverlayView>();
            view.Build(_root.GetComponent<Canvas>());

            return view;
        }
    }
}
