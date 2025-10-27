namespace Flowcast.FlowSave
{
    /// <summary>
    /// Describes a strategy for migrating persisted payloads between versions.
    /// </summary>
    public interface ISaveMigrator
    {
        /// <summary>
        /// Gets the source version handled by this migrator.
        /// </summary>
        SaveVersion FromVersion { get; }

        /// <summary>
        /// Gets the target version produced by this migrator.
        /// </summary>
        SaveVersion ToVersion { get; }

        /// <summary>
        /// Migrates the provided payload from <see cref="FromVersion"/> to <see cref="ToVersion"/>.
        /// </summary>
        /// <param name="data">Raw payload produced by the serializer.</param>
        /// <returns>Updated payload compatible with <see cref="ToVersion"/>.</returns>
        byte[] Migrate(byte[] data);
    }
}
