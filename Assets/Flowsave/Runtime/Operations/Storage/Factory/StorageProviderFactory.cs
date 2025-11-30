using System;

namespace FlowSave.Storage
{
    public sealed class StorageProviderFactory : IStorageProviderFactory
    {
        private readonly StorageOptions _options;
        private readonly IFileNameObfuscator _obfuscator;

        public StorageProviderFactory(StorageOptions options, IFileNameObfuscator obfuscator = null)
        {
            _options = options;
            _obfuscator = obfuscator ?? new Sha256FileNameObfuscator();
        }

        public IStorageProvider CreateStorageProvider(StorageType storageType)
        {
            IStorageProvider provider = storageType switch
            {
                StorageType.FileSystem => new DiskStorageProvider(_options.DiskStorage),
                StorageType.PlayerPrefs => new PlayerPrefsStorageProvider(_options.PlayerPrefsStorage),
                _ => throw new InvalidOperationException($"Unsupported storage provider type: {storageType}")
            };

            // wrap with obfuscation decorator
            if (_options.ObfuscateFileName)
                provider = new ObfuscatingStorageProvider(provider, _obfuscator);

            return provider;
        }
    }

}
