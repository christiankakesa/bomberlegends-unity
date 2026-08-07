using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BomberLegends.Editor
{
    /// <summary>
    /// Guarantees the shaders used only by runtime-created renderers survive into a build.
    /// </summary>
    /// <remarks>
    /// Unity includes a shader when something in a scene or an asset references it. The board and
    /// the player build their renderers at runtime, so nothing references the sprite material at
    /// build time and the shader is stripped — producing a build where the interface draws and the
    /// world does not, while the Editor looks perfectly fine because it always has every shader
    /// loaded.
    /// </remarks>
    public static class ShaderInclusionTool
    {
        /// <summary>Shaders only ever referenced by runtime-created materials.</summary>
        private static readonly string[] RequiredShaders =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit"
        };

        /// <summary>
        /// Shaders that were required by the 2D renderer and are dead weight now.
        /// </summary>
        /// <remarks>
        /// An always-included shader is compiled with every one of its variants, so leaving the 2D
        /// sprite shaders in the list carried tens of megabytes of unused permutations.
        /// </remarks>
        private static readonly string[] ObsoleteShaders =
        {
            "Universal Render Pipeline/2D/Sprite-Unlit-Default",
            "Universal Render Pipeline/2D/Sprite-Lit-Default"
        };

        /// <summary>Adds every shader the runtime renderers need to the always-included list.</summary>
        [MenuItem("Bomber Legends/Rendering/Ensure Runtime Shaders Are Included")]
        public static void EnsureIncluded()
        {
            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/GraphicsSettings.asset").FirstOrDefault();

            if (graphicsSettings == null)
            {
                Debug.LogError("[Shaders] Could not open the graphics settings.");
                return;
            }

            var serialized = new SerializedObject(graphicsSettings);
            var included = serialized.FindProperty("m_AlwaysIncludedShaders");

            var existing = new HashSet<string>();
            for (var i = 0; i < included.arraySize; i++)
            {
                var shader = included.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (shader != null)
                {
                    existing.Add(shader.name);
                }
            }

            var removed = 0;
            for (var i = included.arraySize - 1; i >= 0; i--)
            {
                var shader = included.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (shader == null || System.Array.IndexOf(ObsoleteShaders, shader.name) < 0)
                {
                    continue;
                }

                included.DeleteArrayElementAtIndex(i);
                existing.Remove(shader.name);
                removed++;
                Debug.Log($"[Shaders] Removed '{shader.name}'; the 2D renderer is no longer used.");
            }

            var added = 0;
            foreach (var name in RequiredShaders)
            {
                if (existing.Contains(name))
                {
                    continue;
                }

                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[Shaders] '{name}' was not found in this project; skipping.");
                    continue;
                }

                included.InsertArrayElementAtIndex(included.arraySize);
                included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = shader;
                added++;
                Debug.Log($"[Shaders] Added '{name}' to the always-included list.");
            }

            if (added > 0 || removed > 0)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[Shaders] Done. {added} added, {removed} removed, {existing.Count} already present.");
        }
    }
}
