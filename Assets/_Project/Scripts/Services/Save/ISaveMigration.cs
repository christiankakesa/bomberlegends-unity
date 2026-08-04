namespace BomberLegends.Services.Save
{
    /// <summary>
    /// Upgrades a save payload by exactly one schema version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Migrations are applied in a chain: a payload three versions behind runs three migrations in
    /// order. Each one moves the data from <see cref="FromVersion"/> to the version after it, and
    /// nothing else — a migration that skips ahead cannot be composed and will eventually strand a
    /// player whose save is at an intermediate version.
    /// </para>
    /// <para>
    /// Deserialisation fills fields that did not exist in the older schema with their defaults, so a
    /// migration's job is only to correct values that need more than a default: renamed fields,
    /// rescaled currencies, restructured collections.
    /// </para>
    /// </remarks>
    public interface ISaveMigration
    {
        /// <summary>The schema version this migration upgrades from.</summary>
        int FromVersion { get; }

        /// <summary>Upgrades <paramref name="data"/> in place. The version is stamped by the caller.</summary>
        void Apply(PlayerSaveData data);
    }
}
