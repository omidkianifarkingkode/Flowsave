using System;

namespace Flowsave.Storage
{
    public sealed class StorageProviderFactory : IStorageProviderFactory
    {
        private readonly StorageOptions _options;

        public StorageProviderFactory(StorageOptions options)
        {
            _options = options;
        }

        public IStorageProvider CreateStorageProvider(StorageType storageType)
        {
            return storageType switch
            {
                StorageType.FileSystem => new DiskStorageProvider(_options.DiskStorage),
                StorageType.PlayerPrefs => new PlayerPrefsStorageProvider(_options.PlayerPrefsStorage),
                // later: Cloud, Custom, etc.
                _ => throw new InvalidOperationException($"Unsupported storage provider type: {storageType}"),
            };
        }
    }
}
