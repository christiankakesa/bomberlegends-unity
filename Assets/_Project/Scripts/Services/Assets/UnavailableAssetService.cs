using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BomberLegends.Services.Assets
{
    /// <summary>
    /// The asset service used while no content-loading backend is wired up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Milestone 0 loads no content: scenes come from the build settings and everything else is a
    /// serialized reference. Rather than pull in Addressables — which would add a settings asset, a
    /// group layout and a content-build step to every player build, all for zero assets — the graph
    /// is composed with this, and the real implementation arrives with the first content that needs
    /// it.
    /// </para>
    /// <para>
    /// Every method throws rather than returning null. A silent null here would surface later as an
    /// unexplained missing sprite; an exception names the cause at the call site.
    /// </para>
    /// </remarks>
    public sealed class UnavailableAssetService : IAssetService
    {
        private const string Explanation =
            "No asset-loading backend is wired up yet. Content loading arrives with the first " +
            "feature that needs it; until then, use serialized references on a scene installer.";

        /// <inheritdoc />
        public Awaitable<T> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default)
            where T : Object => throw new NotSupportedException($"Cannot load '{key}'. {Explanation}");

        /// <inheritdoc />
        public Awaitable<GameObject> InstantiateAsync(
            AssetKey key,
            Transform? parent = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException($"Cannot instantiate '{key}'. {Explanation}");

        /// <inheritdoc />
        public Awaitable WarmupAsync(string label, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException($"Cannot warm up '{label}'. {Explanation}");

        /// <inheritdoc />
        public void Release(Object asset) =>
            throw new NotSupportedException($"Nothing was loaded to release. {Explanation}");

        /// <inheritdoc />
        public void ReleaseLabel(string label) =>
            throw new NotSupportedException($"Nothing was loaded to release. {Explanation}");
    }
}
