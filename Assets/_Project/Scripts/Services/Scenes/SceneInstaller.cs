using UnityEngine;

namespace BomberLegends.Services.Scenes
{
    /// <summary>
    /// The single entry point through which a loaded scene receives its dependencies.
    /// </summary>
    /// <remarks>
    /// Every additively loaded scene has exactly one of these on a root object. The scene service
    /// calls <see cref="Install"/> once, immediately after the scene finishes loading and before it
    /// becomes interactive. Scene objects get what they need from here through serialized
    /// references; nothing searches the scene, and there is no static access point to the graph.
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class SceneInstaller : MonoBehaviour
    {
        /// <summary>The scene this installer belongs to. Used to catch a scene wired to the wrong installer.</summary>
        public abstract SceneId Scene { get; }

        /// <summary>Whether <see cref="Install"/> has already run for this instance.</summary>
        public bool IsInstalled { get; private set; }

        /// <summary>
        /// Supplies the object graph to the scene. Called once by the scene service.
        /// </summary>
        /// <param name="context">The composed service graph.</param>
        /// <param name="payload">Data supplied with the transition, or null.</param>
        public void Install(GameContext context, ISceneTransitionPayload? payload)
        {
            if (IsInstalled)
            {
                Debug.LogError($"[Scenes] {GetType().Name} was installed twice. The second call is ignored.");
                return;
            }

            IsInstalled = true;
            OnInstall(context, payload);
        }

        /// <summary>Wires the scene. Called exactly once, after the scene has loaded.</summary>
        protected abstract void OnInstall(GameContext context, ISceneTransitionPayload? payload);
    }
}
