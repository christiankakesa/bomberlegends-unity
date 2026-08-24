using System.Collections;
using System.Reflection;
using BomberLegends.Gameplay.Match;
using BomberLegends.Gameplay.Ui;
using BomberLegends.Input;
using BomberLegends.Services;
using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Items;
using BomberLegends.Simulation.Run;
using BomberLegends.Simulation.Skills;
using BomberLegends.UI.Screens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BomberLegends.Tests.PlayMode
{
    /// <summary>
    /// Holds every greybox screen to a size a phone can actually read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sibling of <see cref="ChoiceCardLegibilityTests"/>, which covers the between-arena
    /// choice. That screen was fixed first because it was the one the gate caught: on touch, none
    /// of three testers could describe the build they were playing, because the card descriptions
    /// were rendering at about 9 dp against a floor of roughly 14.
    /// </para>
    /// <para>
    /// The cause was never confined to that screen. Every view picked its own sizes by eye on a
    /// monitor, where a canvas unit is worth roughly two and a half times what it is worth on the
    /// device — so the pause menu, the control hints, the touch buttons, the pause control and the
    /// hub's way out were all under the floor too, and all of them looked fine to whoever wrote
    /// them. This is the audit of the rest, kept as a test so the next screen cannot rediscover it.
    /// </para>
    /// </remarks>
    public sealed class GreyboxScreenLegibilityTests
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

        /// <summary>
        /// The constant every screen now defers to has to clear the floor it names.
        /// </summary>
        /// <remarks>
        /// Cheap, and it is the assertion the others rest on: once each view says
        /// <c>TextLegibility.MinimumBodySize</c> instead of a number, this is the single place the
        /// arithmetic can still be got wrong.
        /// </remarks>
        [Test]
        public void TheFloorConstantClearsTheFloorItNames()
        {
            Assert.That(TextLegibility.DpFor(TextLegibility.MinimumBodySize),
                Is.GreaterThanOrEqualTo(TextLegibility.MinimumBodyDp),
                "the shared minimum size does not itself reach the minimum dp it exists to enforce");

            // The other half of the claim: one unit smaller does not clear it, so the constant is
            // the smallest usable value rather than an arbitrary large one.
            Assert.That(TextLegibility.DpFor(TextLegibility.MinimumBodySize - 1),
                Is.LessThan(TextLegibility.MinimumBodyDp),
                $"{TextLegibility.MinimumBodySize - 1} units now clears the floor too, so the " +
                "conversion has changed and every comment quoting these numbers is stale");
        }

        [UnityTest]
        public IEnumerator ThePauseMenuIsReadableOnAPhone()
        {
            var canvas = BuildCanvas(out var scale);

            var menu = _root!.AddComponent<PauseMenuView>();
            menu.Build(canvas);
            menu.Show();

            yield return null;

            AssertEveryWordIsReadable(_root, scale, "the pause menu");
        }

        [UnityTest]
        public IEnumerator TheControlHintsAreReadableOnAPhone()
        {
            // Both schemes, because they do not print the same strings: the pad line names the
            // stick as well as the button for shooting, which makes it the longest text this panel
            // ever has to hold.
            foreach (var scheme in new[] { ControlScheme.KeyboardMouse, ControlScheme.Gamepad })
            {
                var canvas = BuildCanvas(out var scale);
                var hints = BuildHints(canvas, scheme);

                yield return null;

                AssertEveryWordIsReadable(_root!, scale, $"the control hints on {scheme}");

                // The panel teaches the controls; if its own text spills out of it, the binding
                // that got clipped is the one nobody learns.
                AssertLabelsFitTheirRects(hints.gameObject, $"the control hints on {scheme}");

                TearDown();
            }
        }

        [UnityTest]
        public IEnumerator TheTouchControlsAreReadableOnAPhone()
        {
            var canvas = BuildCanvas(out var scale);
            var anchor = BuildBombButtonAnchor(canvas);

            var loadout = SkillLoadout.Of(
                new SkillSlot { Id = SkillId.Dash, MaxCharges = 1, Charges = 1 },
                new SkillSlot { Id = SkillId.Skillshot, MaxCharges = 1, Charges = 1 });

            // Two skills of three, so the cluster draws its empty slot as well. Touch is the one
            // input where the word on the control is all there is, and that goes double for a
            // control that does nothing yet.
            TouchControlsBuilder.Build(anchor, loadout, out _);

            yield return null;

            // Touch is the one input where the word on the control is all there is. There is no key
            // to recognise and no pad glyph to learn, so an unreadable label leaves a coloured
            // circle that does something unknown — which is the whole reason these were audited.
            AssertEveryWordIsReadable(_root!, scale, "the touch controls");
            AssertLabelsFitTheirRects(_root!, "the touch controls");
        }

        [UnityTest]
        public IEnumerator TheHubQuitControlIsReadableOnAPhone()
        {
            var canvas = BuildCanvas(out var scale);

            var quit = BuildHubQuitButton(canvas);
            Assert.That(quit, Is.Not.Null, "the hub built no quit control to measure");

            yield return null;

            AssertEveryWordIsReadable(quit!.gameObject, scale, "the hub's quit control");
        }

        [UnityTest]
        public IEnumerator TheInMatchPauseControlIsReadableOnAPhone()
        {
            var canvas = BuildCanvas(out var scale);

            // The scene authors this at the size the hub's PLAY needs; the match shrinks it in
            // place. Shrinking a control is fine — shrinking the word inside it is what was not.
            var button = GreyboxUi.CreateButton(
                canvas.transform, "QUIT", Vector2.zero, new Vector2(420f, 120f), Color.grey);

            ShrinkToPauseControl(button);

            yield return null;

            AssertEveryWordIsReadable(button.gameObject, scale, "the in-match pause control");
            AssertLabelsFitTheirRects(button.gameObject, "the in-match pause control");
        }

        /// <summary>
        /// The readout that is on screen for the whole match, at its longest.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The one screen this audit had missed, and the one with the most to lose: it is drawn
        /// over the arena for every second of play, and it is a single line that grows every time
        /// something is added to it. It has already been clipped once on a device — it wrapped onto
        /// a second line the box then cut off, taking the charges and the whole build with it —
        /// which is why the view forces overflow. Overflow trades a clipped line for one that can
        /// run off the side instead, so the length is what has to be held.
        /// </para>
        /// <para>
        /// Measured on a real run rather than an authored string: a bomb on the board, both skills
        /// spent, two items held and an arena number in double digits is the fullest the line gets
        /// today, and every part of it comes from the code that ships.
        /// </para>
        /// </remarks>
        [UnityTest]
        public IEnumerator TheMatchReadoutFitsEveryScreenAtItsLongest()
        {
            // The device the floor comes from goes first, on its own: dp is a claim about a real
            // screen, and the other two shapes in the set are aspect ratios rather than handsets.
            _root = PhoneCanvas.Build(PhoneCanvas.GalaxyS21Ultra, out var deviceScale);
            BuildFullestReadout(_root.GetComponent<Canvas>());

            yield return null;

            AssertEveryWordIsReadable(_root, deviceScale, "the match readout");
            TearDown();

            foreach (var shape in PhoneCanvas.Shapes)
            {
                _root = PhoneCanvas.Build(shape, out var scale);

                var label = BuildFullestReadout(_root.GetComponent<Canvas>())
                    .GetComponent<Text>();

                yield return null;

                // The label is inset 40 units from the left, and the top-right corner of a match
                // holds the pause control, so the same inset is the least that has to stay clear
                // on the other side.
                const float Inset = 40f;
                var available = shape.x / scale - Inset * 2f;

                Assert.That(label.preferredWidth, Is.LessThanOrEqualTo(available),
                    $"on a {shape.x}x{shape.y} screen the readout needs " +
                    $"{label.preferredWidth:F0} units and has {available:F0}: " +
                    $"\"{label.text}\"");

                TearDown();
            }
        }

        [UnityTest]
        public IEnumerator NothingOnTheseScreensRunsOffTheNarrowestCanvas()
        {
            // Shared with the choice-card test, and worth reading before adding a screen: the
            // tightest canvas across is the tablet, not a phone. See PhoneCanvas.Shapes.
            foreach (var shape in PhoneCanvas.Shapes)
            {
                _root = PhoneCanvas.Build(shape, out var scale);
                var canvas = _root.GetComponent<Canvas>();

                var menu = _root.AddComponent<PauseMenuView>();
                menu.Build(canvas);
                menu.Show();

                BuildHints(canvas, ControlScheme.Gamepad);

                yield return null;

                AssertEverythingIsOnScreen(_root, shape, scale);

                TearDown();
            }
        }

        /// <summary>
        /// Fails with the size that broke, for every piece of text beneath the given object.
        /// </summary>
        /// <remarks>
        /// Inactive objects are measured too. These screens spend most of their life hidden — the
        /// pause menu is built hidden, the cancel zone only appears mid-drag — and a font size that
        /// is wrong while hidden is wrong when it is shown.
        /// </remarks>
        private static void AssertEveryWordIsReadable(GameObject root, float scale, string screen)
        {
            var labels = root.GetComponentsInChildren<Text>(includeInactive: true);

            Assert.That(labels, Is.Not.Empty, $"{screen} drew no text at all, so nothing was checked");

            foreach (var label in labels)
            {
                var dp = PhoneCanvas.DpOf(label.fontSize, scale);
                var what = string.IsNullOrEmpty(label.text) ? label.name : $"\"{label.text}\"";

                Assert.That(dp, Is.GreaterThanOrEqualTo(TextLegibility.MinimumBodyDp),
                    $"on {screen}, {what} renders at {dp:F1} dp, under the " +
                    $"{TextLegibility.MinimumBodyDp} dp floor; this is the failure that made touch " +
                    "testers choose at random");
            }
        }

        /// <summary>Fails when a label needs more width than the rect it was given.</summary>
        private static void AssertLabelsFitTheirRects(GameObject root, string screen)
        {
            foreach (var label in root.GetComponentsInChildren<Text>(includeInactive: true))
            {
                if (string.IsNullOrEmpty(label.text))
                {
                    continue;
                }

                var width = label.rectTransform.rect.width;

                Assert.That(label.preferredWidth, Is.LessThanOrEqualTo(width),
                    $"on {screen}, \"{label.text}\" needs {label.preferredWidth:F0} units but was " +
                    $"given {width:F0}; raising a font size without growing its rect moves the " +
                    "problem rather than fixing it");
            }
        }

        /// <summary>
        /// Fails when any drawn thing reaches past the edge of the screen.
        /// </summary>
        /// <remarks>
        /// The canvas is measured from the resolution rather than read back off its own
        /// RectTransform. A screen-space overlay canvas overwrites that rect with the real window
        /// every frame, so after a single yield it describes the batch-mode editor window and not
        /// the phone being simulated — which reads as every wide panel running off the side.
        /// </remarks>
        private static void AssertEverythingIsOnScreen(GameObject root, Vector2 shape, float scale)
        {
            var half = shape / scale * 0.5f;
            var corners = new Vector3[4];

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(includeInactive: false))
            {
                graphic.rectTransform.GetWorldCorners(corners);

                for (var i = 0; i < corners.Length; i++)
                {
                    var local = root.transform.InverseTransformPoint(corners[i]);

                    Assert.That(Mathf.Abs(local.x), Is.LessThanOrEqualTo(half.x + 0.5f),
                        $"{graphic.name} reaches {Mathf.Abs(local.x):F0} units from centre, past " +
                        $"the {half.x:F0} a {shape.x}x{shape.y} screen has");

                    Assert.That(Mathf.Abs(local.y), Is.LessThanOrEqualTo(half.y + 0.5f),
                        $"{graphic.name} reaches {Mathf.Abs(local.y):F0} units from centre, past " +
                        $"the {half.y:F0} a {shape.x}x{shape.y} screen has");
                }
            }
        }

        /// <summary>
        /// Draws the readout at its longest: arena in double digits, a bomb on the board, both
        /// skills spent and the two roomiest items held.
        /// </summary>
        private static MatchHudView BuildFullestReadout(Canvas canvas)
        {
            var hud = BuildMatchReadout(canvas);
            hud.ArenaNumber = 12;

            var run = new GameRun(
                SimulationConfig.Default,
                new[] { CrowdedArena },
                seed: 1u,
                startingItems: TheTwoLongestItemNames);

            // One tick spends both skills and puts the only bomb on the board, so all three
            // counters show a countdown rather than a digit.
            run.Current.Tick(new PlayerIntent(
                0, 0, IntentButtons.Bomb | IntentButtons.Skill1 | IntentButtons.Skill2));

            hud.Render(run.Current);

            return hud;
        }

        /// <summary>An arena crowded enough to put two digits in the enemy count.</summary>
        private static LevelLayout CrowdedArena => LevelLayout.Parse(
            "###############",
            "#EEEEEEEEEEEE.#",
            "#.............#",
            "#......P......#",
            "#.............#",
            "###############");

        /// <summary>
        /// The two items whose names take the most room, which is what the line has to survive.
        /// </summary>
        /// <remarks>
        /// Read out of the catalogue rather than named here, so an item renamed or added is
        /// measured without anybody remembering to come back to this test.
        /// </remarks>
        private static ItemId[] TheTwoLongestItemNames
        {
            get
            {
                var all = (ItemId[])ItemCatalog.All.Clone();
                System.Array.Sort(all, (a, b) =>
                    ItemCatalog.Name(b).Length.CompareTo(ItemCatalog.Name(a).Length));

                return new[] { all[0], all[1] };
            }
        }

        /// <summary>
        /// Builds the match readout the way the scene authors it.
        /// </summary>
        /// <remarks>
        /// Duplicated from <c>SceneScaffolder.CreateHud</c> for the reason <see cref="PhoneCanvas"/>
        /// gives for duplicating the scaler: the scaffolder is editor-only, and if the two drift
        /// this stops measuring the shipped readout — which is worth a comment on both sides.
        /// </remarks>
        private static MatchHudView BuildMatchReadout(Canvas canvas)
        {
            var host = new GameObject("Readout", typeof(Text), typeof(MatchHudView));
            host.transform.SetParent(canvas.transform, false);

            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -30f);
            rect.sizeDelta = new Vector2(1700f, 70f);

            var text = host.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 42;
            text.alignment = TextAnchor.UpperLeft;

            var field = typeof(MatchHudView).GetField(
                "_output", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "MatchHudView no longer has an _output to write to");
            field!.SetValue(host.GetComponent<MatchHudView>(), text);

            return host.GetComponent<MatchHudView>();
        }

        private Canvas BuildCanvas(out float scale)
        {
            _root = PhoneCanvas.Build(PhoneCanvas.GalaxyS21Ultra, out scale);
            return _root.GetComponent<Canvas>();
        }

        private static ControlHintsView BuildHints(Canvas canvas, ControlScheme scheme)
        {
            var host = new GameObject("ControlHintsHost");
            host.transform.SetParent(canvas.transform, false);

            var devices = new ControlSchemeTracker();

            // Forced rather than pressed, because a test cannot press anything. Forcing also marks
            // the tracker as used, which is what stops the panel from hiding itself behind the
            // touch controls it defers to.
            devices.ForceScheme(scheme);

            var hints = host.AddComponent<ControlHintsView>();
            hints.Begin(canvas, devices);
            hints.Render();

            return hints;
        }

        private static RectTransform BuildBombButtonAnchor(Canvas canvas)
        {
            var host = new GameObject("BombButton", typeof(RectTransform), typeof(Image));
            host.transform.SetParent(canvas.transform, false);

            // The size and corner the scene authors the bomb button at. The skill cluster is laid
            // out from these, so measuring against anything else would measure the test.
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(260f, 260f);
            rect.anchoredPosition = new Vector2(-60f, 60f);

            return rect;
        }

        /// <summary>
        /// Builds the hub's quit control the way the hub does.
        /// </summary>
        /// <remarks>
        /// Reached by reflection because the real entry point is <c>OnInstall</c>, which wants a
        /// live game context and a scene service to reach one line of text. The alternative was to
        /// leave the one control that gets a player out of the game unmeasured, which is how it
        /// came to be at 13 dp.
        /// </remarks>
        private static Button? BuildHubQuitButton(Canvas canvas)
        {
            var host = new GameObject("HubInstaller");
            host.transform.SetParent(canvas.transform, false);

            var play = GreyboxUi.CreateButton(
                canvas.transform, "PLAY", Vector2.zero, new Vector2(420f, 120f), Color.cyan);

            var installer = host.AddComponent<HubInstaller>();

            var field = typeof(HubInstaller).GetField(
                "_playButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "HubInstaller no longer has a _playButton to point at");
            field!.SetValue(installer, play);

            var method = typeof(HubInstaller).GetMethod(
                "CreateQuitButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "HubInstaller no longer builds its own quit control");

            return (Button?)method!.Invoke(installer, null);
        }

        /// <summary>Applies the match's in-place shrink of the shared menu button.</summary>
        /// <remarks>Private static in the installer, for the same reason as above.</remarks>
        private static void ShrinkToPauseControl(Button button)
        {
            var method = typeof(MatchInstaller).GetMethod(
                "ShrinkToPauseControl", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "the match no longer shrinks the shared button");

            method!.Invoke(null, new object[] { button });
        }
    }
}
