using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BomberLegends.Services.Assets
{
    /// <summary>
    /// Identifies a loadable asset.
    /// </summary>
    /// <remarks>
    /// A typed wrapper around the address string rather than a raw <see cref="string"/>, so an
    /// asset key cannot be passed where a label or a scene name is expected. Deliberately not
    /// Addressables' <c>AssetReference</c>: keeping the loading library out of the service contract
    /// is what lets the strategy change without touching a single feature.
    /// </remarks>
    public readonly struct AssetKey : IEquatable<AssetKey>
    {
        /// <summary>The address the asset is registered under.</summary>
        public readonly string Address;

        /// <summary>Creates a key for the given address.</summary>
        /// <exception cref="ArgumentException">The address is null, empty or whitespace.</exception>
        public AssetKey(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Asset address must not be empty.", nameof(address));
            }

            Address = address;
        }

        /// <summary>Returns <see langword="true"/> when both keys point at the same address.</summary>
        public bool Equals(AssetKey other) => string.Equals(Address, other.Address, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is AssetKey other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Address.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Address;
    }

    /// <summary>
    /// Loads and releases content.
    /// </summary>
    /// <remarks>
    /// Gameplay code never references the underlying loading library. Everything a match needs is
    /// preloaded during the loading screen; nothing is loaded while a match is running, because a
    /// hitch mid-blast is indistinguishable from a bug. Every load must be matched by a
    /// <see cref="Release"/>: a leaked handle on mobile is an out-of-memory crash, not a warning.
    /// </remarks>
    public interface IAssetService
    {
        /// <summary>Loads a single asset.</summary>
        Awaitable<T> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default)
            where T : Object;

        /// <summary>Loads and instantiates a prefab under <paramref name="parent"/>.</summary>
        Awaitable<GameObject> InstantiateAsync(
            AssetKey key,
            Transform? parent = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Preloads everything tagged with <paramref name="label"/>. Called from the loading screen
        /// so a match never loads content while it is being played.
        /// </summary>
        Awaitable WarmupAsync(string label, CancellationToken cancellationToken = default);

        /// <summary>Releases an asset previously returned by this service.</summary>
        void Release(Object asset);

        /// <summary>Releases everything loaded under <paramref name="label"/>.</summary>
        void ReleaseLabel(string label);
    }
}
