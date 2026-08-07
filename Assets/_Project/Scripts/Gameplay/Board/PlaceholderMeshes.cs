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
        public static Mesh Cube => _cube ??= Extract(PrimitiveType.Cube);

        /// <summary>A unit sphere.</summary>
        public static Mesh Sphere => _sphere ??= Extract(PrimitiveType.Sphere);

        /// <summary>A unit quad, used for anything that lies flat on the ground.</summary>
        public static Mesh Quad => _quad ??= Extract(PrimitiveType.Quad);

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
        /// Pulls the mesh out of a temporary primitive.
        /// </summary>
        /// <remarks>
        /// The primitive itself is discarded immediately. It also arrives with a collider, which is
        /// removed: this project resolves every collision on the grid and uses no physics at all.
        /// </remarks>
        private static Mesh Extract(PrimitiveType type)
        {
            var temporary = GameObject.CreatePrimitive(type);
            var mesh = temporary.GetComponent<MeshFilter>().sharedMesh;

            if (Application.isPlaying)
            {
                Object.Destroy(temporary);
            }
            else
            {
                Object.DestroyImmediate(temporary);
            }

            return mesh;
        }
    }
}
