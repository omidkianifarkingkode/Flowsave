namespace FlowSave.Storage
{
    /// <summary>
    /// Factory bound to a specific FlowSave config snapshot.
    /// </summary>
    public interface IStorageProviderFactory
    {
        IStorageProvider CreateStorageProvider(StorageType storageType);
    }
}
