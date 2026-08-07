using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BomberLegends.Editor
{
    /// <summary>
    /// Points the project at the 3D render pipeline asset.
    /// </summary>
    /// <remarks>
    /// The project began on URP's 2D renderer. The v2.0 concept revision moved it to low-poly 3D,
    /// which needs the forward renderer instead. Done in code rather than by hand so a fresh
    /// checkout ends up in the same state, and so the swap is reviewable as a diff.
    /// </remarks>
    public static class RenderPipelineTool
    {
        private const string PipelinePath = "Assets/_Project/Settings/Rendering/PC_RPAsset.asset";

        /// <summary>Assigns the 3D pipeline asset as the project default and to every quality level.</summary>
        [MenuItem("Bomber Legends/Rendering/Use 3D Pipeline")]
        public static void UseThreeDimensional()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                Debug.LogError($"[Rendering] Could not load the pipeline asset at {PipelinePath}.");
                return;
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;

            var originalQuality = QualitySettings.GetQualityLevel();
            for (var level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }

            QualitySettings.SetQualityLevel(originalQuality, applyExpensiveChanges: false);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Rendering] Default pipeline is now {pipeline.name}.");
        }
    }
}
