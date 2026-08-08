using UnityEngine;

namespace BomberLegends.Gameplay.Board
{
    /// <summary>
    /// The primitives and materials the greybox is built from.
    /// </summary>
    /// <remarks>
    /// The slice deliberately ships with no authored art: models are the most expensive asset in the
    /// project and must not be produced against mechanics that have not been validated. Building the
    /// greybox from primitives also keeps the repository free of binary assets while the hybrid is
    /// being proven.
    /// </remarks>
    public static class PlaceholderMeshes
    {
        private static Mesh? _cube;
        private static Mesh? _sphere;
        private static Mesh? _quad;
        private static Shader? _shader;

        /// <summary>A unit cube.</summary>
        public static Mesh Cube => _cube ??= Load("Cube.fbx");

        /// <summary>A unit sphere.</summary>
        public static Mesh Sphere => _sphere ??= Load("Sphere.fbx");

        /// <summary>A unit quad, used for anything that lies flat on the ground.</summary>
        public static Mesh Quad => _quad ??= Load("Quad.fbx");

        /// <summary>Creates an opaque lit material in the given colour.</summary>
        public static Material CreateMaterial(Color colour)
        {
            _shader ??= Shader.Find("Universal Render Pipeline/Lit");

            var material = new Material(_shader)
            {
                color = colour,
                hideFlags = HideFlags.HideAndDontSave
            };

            // Flat and matte: the greybox is for reading shapes, not for showing off lighting.
            material.SetFloat("_Smoothness", 0f);
            return material;
        }

        /// <summary>Creates a transparent lit material, for effects that fade out.</summary>
        public static Material CreateTransparentMaterial(Color colour)
        {
            var material = CreateMaterial(colour);

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            return material;
        }

        /// <summary>
        /// Loads one of the engine's built-in meshes by name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Deliberately not <see cref="GameObject.CreatePrimitive"/>, which was used here until it
        /// failed on device. That call attaches a collider, and <b>the Physics module is stripped
        /// from a player build</b> because this project resolves every collision on the grid and
        /// references physics nowhere else. The result was
        /// <c>Can't add component because class 'MeshCollider' doesn't exist!</c> for every mesh,
        /// on every arena build — invisible in the Editor, where nothing is ever stripped.
        /// </para>
        /// <para>
        /// The meshes it produced were fine, so the game still rendered. It simply filled the log
        /// and forced the development console over the play area. Loading the built-in mesh
        /// directly asks for the one thing actually wanted and pulls in no module at all.
        /// </para>
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">The engine has no such mesh.</exception>
        private static Mesh Load(string name)
        {
            var mesh = Resources.GetBuiltinResource<Mesh>(name);

            // Nothing downstream can proceed without it, and every caller would otherwise fail with
            // a null reference several frames later and nowhere near the cause.
            return mesh != null
                ? mesh
                : throw new System.InvalidOperationException(
                    $"The built-in mesh '{name}' could not be loaded, so the greybox cannot draw.");
        }
    }
}
