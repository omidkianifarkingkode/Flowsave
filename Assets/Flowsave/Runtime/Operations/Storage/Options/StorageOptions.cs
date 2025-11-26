using System;

namespace Flowsave.Storage
{
    [Serializable]
    public class DefaultStorageOptions
    {
        public StorageType StorageType = StorageType.FileSystem;
        public bool ObfuscateFileName = false;
        public DiskStorageOptions DiskStorage = new();
        public PlayerPrefsStorageOptions PlayerPrefsStorage = new();
    }

    [Serializable]
    public class StorageOptions : DefaultStorageOptions
    {
        public bool UseDefault = true;

        public static StorageOptions Clone(DefaultStorageOptions from) =>
            from == null ? null : new StorageOptions
            {
                UseDefault = true, // ALWAYS true when cloning from defaults

                ObfuscateFileName = from.ObfuscateFileName,
                StorageType = from.StorageType,
                DiskStorage = DiskStorageOptions.Clone(from.DiskStorage),
                PlayerPrefsStorage = PlayerPrefsStorageOptions.Clone(from.PlayerPrefsStorage)
            };

        public static StorageOptions Clone(StorageOptions from) =>
            from == null ? null : new StorageOptions
            {
                UseDefault = from.UseDefault,

                ObfuscateFileName = from.ObfuscateFileName,
                StorageType = from.StorageType,
                DiskStorage = DiskStorageOptions.Clone(from.DiskStorage),
                PlayerPrefsStorage = PlayerPrefsStorageOptions.Clone(from.PlayerPrefsStorage)
            };

    }
}
