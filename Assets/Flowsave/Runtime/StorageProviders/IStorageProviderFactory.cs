using Flowsave.Configurations;
using System;

namespace FlowSave
{
    public interface IStorageProviderFactory
    {
        IStorageProvider CreateStorageProvider(SaveProviderType providerType);
    }

    public class StorageProviderFactory : IStorageProviderFactory
    {
        public IStorageProvider CreateStorageProvider(SaveProviderType providerType)
        {
            return providerType switch
            {
                SaveProviderType.FileSystem => new FileStorageProvider(),
                SaveProviderType.PlayerPrefs => new PlayerPrefsStorageProvider(),
                _ => throw new InvalidOperationException($"Unsupported storage provider type: {providerType}")
            };
        }
    }
}
