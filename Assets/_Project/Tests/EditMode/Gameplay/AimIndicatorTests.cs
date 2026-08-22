using BomberLegends.Core;
using BomberLegends.Gameplay.Skills;
using BomberLegends.Input;
using BomberLegends.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers the ground arrow that shows a touch or gamepad player where their shot will go.
    /// </summary>
    /// <remarks>
    /// Worth testing rather than eyeballing because two of the three things it has to get right are
    /// invisible until they are wrong on a device that is not to hand: which way the mesh faces —
    /// wound the other way it simply is not drawn, from the only angle anyone ever views it from —
    /// and whether the aim in the intent survives the trip into a world rotation.
    /// </remarks>
    public sealed class AimIndicatorTests
    {
        private Mesh? _mesh;

        [TearDown]
        public void TearDown()
        {
            if (_mesh != null)
            {
                Object.DestroyImmediate(_mesh);
                _mesh = null;
            }
        }

        [Test]
        public void TheArrowIsDrawnWhenTouchOrGamepadSuppliesAnAim()
        {
            var aimed = new PlayerIntent(0, 0, IntentButtons.None, 100, 0);

            Assert.That(AimIndicatorView.ShouldShow(aimed, ControlScheme.Touch), Is.True);
            Assert.That(AimIndicatorView.ShouldShow(aimed, ControlScheme.Gamepad), Is.True);
        }

        [Test]
        public void TheArrowStaysHiddenForAMouseThatAlreadyHasACursor()
        {
            var aimed = new PlayerIntent(0, 0, IntentButtons.None, 100, 0);

            Assert.That(AimIndicatorView.ShouldShow(aimed, ControlScheme.KeyboardMouse), Is.False);
        }

        [Test]
        public void TheArrowStaysHiddenWhileNobodyIsAiming()
        {
            // Moving is not aiming. Left stick travel must not light the arrow up, or it is on for
            // the whole run and stops meaning anything.
            var moving = new PlayerIntent(100, 100, IntentButtons.None);

            Assert.That(AimIndicatorView.ShouldShow(moving, ControlScheme.Gamepad), Is.False);
        }

        [Test]
        public void TheArrowPointsWhereTheAimPoints()
        {
            // Grid Y runs along world Z, so an aim up the board is an arrow along +Z.
            var north = AimIndicatorView.RotationFor(new PlayerIntent(0, 0, IntentButtons.None, 0, 100));
            var east = AimIndicatorView.RotationFor(new PlayerIntent(0, 0, IntentButtons.None, 100, 0));

            AssertPointsAlong(north, Vector3.forward);
            AssertPointsAlong(east, Vector3.right);
        }

        [Test]
        public void ADiagonalAimIsNotSnappedToACardinal()
        {
            // The whole point of a free-aim skillshot. A rounded-off arrow would be lying about
            // where the shot goes, which is worse than drawing nothing.
            var rotation = AimIndicatorView.RotationFor(
                new PlayerIntent(0, 0, IntentButtons.None, 71, 71));

            var forward = rotation * Vector3.forward;

            Assert.That(forward.x, Is.EqualTo(forward.z).Within(0.01f));
            Assert.That(forward.x, Is.GreaterThan(0.5f));
        }

        [Test]
        public void TheArrowLiesFlatAndFacesUpwards()
        {
            _mesh = AimIndicatorView.CreateArrowMesh();

            foreach (var vertex in _mesh.vertices)
            {
                Assert.That(vertex.y, Is.EqualTo(0f), "The arrow is a ground decal; nothing may lift off the floor.");
            }

            foreach (var normal in _mesh.normals)
            {
                Assert.That(normal, Is.EqualTo(Vector3.up));
            }
        }

        [Test]
        public void EveryTriangleIsWoundToBeVisibleFromAbove()
        {
            _mesh = AimIndicatorView.CreateArrowMesh();

            var vertices = _mesh.vertices;
            var triangles = _mesh.triangles;

            Assert.That(triangles.Length, Is.EqualTo(9), "A shaft of two triangles and a head of one.");

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];

                // Seen from above, x runs right and z runs up the screen, and Unity treats a
                // clockwise winding as the front face — which is a negative signed area here.
                var area = ((b.x - a.x) * (c.z - a.z)) - ((c.x - a.x) * (b.z - a.z));

                Assert.That(
                    area,
                    Is.LessThan(0f),
                    $"Triangle {i / 3} is wound away from the camera and would not be drawn.");
            }
        }

        [Test]
        public void TheHeadIsWiderThanTheShaftSoItReadsAsAnArrow()
        {
            _mesh = AimIndicatorView.CreateArrowMesh();

            var vertices = _mesh.vertices;
            var widest = 0f;
            var tip = 0f;

            foreach (var vertex in vertices)
            {
                widest = Mathf.Max(widest, Mathf.Abs(vertex.x));
                tip = Mathf.Max(tip, vertex.z);
            }

            // The tip is the single vertex furthest along the arrow and sits on the centre line.
            foreach (var vertex in vertices)
            {
                if (Mathf.Approximately(vertex.z, tip))
                {
                    Assert.That(vertex.x, Is.EqualTo(0f).Within(0.0001f));
                }
            }

            Assert.That(widest, Is.GreaterThan(0.3f), "A thin arrow is the problem being fixed.");
        }

        /// <summary>Asserts the mesh's own +Z ends up along the given world direction.</summary>
        private static void AssertPointsAlong(Quaternion rotation, Vector3 expected)
        {
            var actual = rotation * Vector3.forward;

            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f), $"Expected {expected}, got {actual}.");
        }
    }
}
