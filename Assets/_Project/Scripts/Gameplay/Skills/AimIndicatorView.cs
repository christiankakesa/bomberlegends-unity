using BomberLegends.Gameplay.Board;
using BomberLegends.Gameplay.Match;
using BomberLegends.Gameplay.Player;
using BomberLegends.Input;
using BomberLegends.Simulation;
using UnityEngine;

namespace BomberLegends.Gameplay.Skills
{
    /// <summary>
    /// Draws a fat arrow on the ground showing where the next skillshot will go.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asked for outright in round two: <i>"I need a fat arrow on the ground oriented to the enemy
    /// when shooting."</i> An aim indicator already existed, but it was drawn on the skill button —
    /// which on a phone is the one place guaranteed to be underneath a thumb, and on a pad does not
    /// exist at all. Three testers reported the consequence three different ways: "I couldn't tell
    /// where my finger landed", "it fired at the wrong target", "the controls are fighting my
    /// thumb".
    /// </para>
    /// <para>
    /// It reads the aim from <see cref="MatchRunner.LastIntent"/> rather than from any one device,
    /// so a thumb drag and a right stick light it up through the same two bytes. Nothing here can
    /// affect the rules: the aim was going to be sent to the simulation either way.
    /// </para>
    /// <para>
    /// Hidden for mouse play, where the cursor is already an answer to the same question and a
    /// second one would only be clutter. Round two also gives no reason to add it there — keyboard
    /// players hit every aiming metric.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AimIndicatorView : MonoBehaviour
    {
        /// <summary>Matches the projectile, so the arrow and the shot read as one thing.</summary>
        private static readonly Color Colour = new Color(1f, 0.45f, 0.92f, 0.5f);

        /// <summary>How far in front of the player the arrow starts, in tiles.</summary>
        /// <remarks>Clear of the body, which is 0.7 tiles across, so it never looks impaled.</remarks>
        private const float StartTiles = 0.5f;

        /// <summary>Length of the shaft, in tiles.</summary>
        private const float ShaftTiles = 1.9f;

        /// <summary>Length of the head, in tiles.</summary>
        private const float HeadTiles = 0.85f;

        /// <summary>Half-width of the shaft, in tiles. Fat, as requested.</summary>
        private const float ShaftHalfWidth = 0.17f;

        /// <summary>Half-width of the head where it meets the shaft, in tiles.</summary>
        private const float HeadHalfWidth = 0.38f;

        /// <summary>Clear of the floor, so it never fights the ground plane for depth.</summary>
        private const float Hover = 0.03f;

        private MatchRunner? _runner;
        private ControlSchemeTracker? _devices;
        private Transform? _arrow;
        private Mesh? _mesh;
        private Material? _material;

        /// <summary>Builds the arrow under the player and wires it to what it reads.</summary>
        public void Begin(
            MatchRunner runner,
            PlayerView player,
            BoardProjector projector,
            ControlSchemeTracker devices)
        {
            _runner = runner;
            _devices = devices;

            _arrow ??= Build(player.transform, projector.TileSize);

            Show(false);
        }

        /// <summary>
        /// Points the arrow, after the player has been placed for this frame.
        /// </summary>
        /// <remarks>
        /// In <c>LateUpdate</c> for the same reason the camera follows there: the runner moves the
        /// player in <c>Update</c>, and reading the position before that leaves the arrow a frame
        /// behind its own owner.
        /// </remarks>
        private void LateUpdate()
        {
            if (_runner == null || _arrow == null)
            {
                return;
            }

            var intent = _runner.LastIntent;

            if (_devices == null || !ShouldShow(intent, _devices.Current))
            {
                Show(false);
                return;
            }

            // The arrow is parented to the player, whose transform is never rotated, so a local
            // rotation is a world one. Following the player costs nothing at all this way.
            _arrow.localRotation = RotationFor(intent);

            Show(true);
        }

        /// <summary>
        /// Whether an aim is worth drawing on this device.
        /// </summary>
        /// <remarks>
        /// Touch and gamepad both aim blind — a thumb covers the button, a stick has no cursor.
        /// A mouse does not, and drawing a second answer beside the cursor is only clutter.
        /// </remarks>
        internal static bool ShouldShow(PlayerIntent intent, ControlScheme scheme) =>
            intent.HasAim && scheme != ControlScheme.KeyboardMouse;

        /// <summary>
        /// Turns an aim into the rotation that points the arrow along it.
        /// </summary>
        /// <remarks>
        /// The board is projected onto world XZ with grid Y running along Z, and the projection
        /// scales both axes alike — so a grid aim is already a world direction once normalised.
        /// The mesh is authored pointing +Z, which is what makes this one look rotation.
        /// </remarks>
        internal static Quaternion RotationFor(PlayerIntent intent)
        {
            var direction = new Vector3(intent.AimX, 0f, intent.AimY);

            return direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private Transform Build(Transform player, float tileSize)
        {
            _mesh = CreateArrowMesh();
            _material = PlaceholderMeshes.CreateTransparentMaterial(Colour);

            var child = new GameObject("AimIndicator", typeof(MeshFilter), typeof(MeshRenderer));

            // Parented to the player rather than positioned each frame: the arrow always starts at
            // the feet, so there is no second copy of the interpolation to keep in step.
            child.transform.SetParent(player, false);
            child.transform.localPosition = new Vector3(0f, Hover, 0f);

            // Authored in tiles, drawn in world units.
            child.transform.localScale = Vector3.one * tileSize;

            child.GetComponent<MeshFilter>().sharedMesh = _mesh;

            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return child.transform;
        }

        /// <summary>
        /// Builds the arrow: a rectangular shaft and a triangular head, lying flat and pointing +Z.
        /// </summary>
        /// <remarks>
        /// Generated rather than assembled from the placeholder quads. Two quads would have to be
        /// rotated into a rhombus to suggest a point, which reads as a diamond rather than an
        /// arrow — and the request was specifically for something that says <i>which way</i>.
        /// Lying in the XZ plane already means no flattening rotation, so aiming it is one
        /// <see cref="Quaternion.LookRotation(Vector3, Vector3)"/> and nothing else.
        /// </remarks>
        internal static Mesh CreateArrowMesh()
        {
            var neck = StartTiles + ShaftTiles;
            var tip = neck + HeadTiles;

            var vertices = new[]
            {
                new Vector3(-ShaftHalfWidth, 0f, StartTiles),
                new Vector3(ShaftHalfWidth, 0f, StartTiles),
                new Vector3(ShaftHalfWidth, 0f, neck),
                new Vector3(-ShaftHalfWidth, 0f, neck),
                new Vector3(-HeadHalfWidth, 0f, neck),
                new Vector3(HeadHalfWidth, 0f, neck),
                new Vector3(0f, 0f, tip)
            };

            // Wound clockwise as seen from above, which is what Unity treats as the front face —
            // the other way round the arrow is invisible from the only angle anyone views it from.
            var triangles = new[] { 0, 3, 2, 0, 2, 1, 4, 6, 5 };

            var normals = new Vector3[vertices.Length];

            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.up;
            }

            var mesh = new Mesh
            {
                name = "AimArrow",
                hideFlags = HideFlags.HideAndDontSave
            };

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();

            return mesh;
        }

        private void Show(bool visible)
        {
            if (_arrow != null && _arrow.gameObject.activeSelf != visible)
            {
                _arrow.gameObject.SetActive(visible);
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }

            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
