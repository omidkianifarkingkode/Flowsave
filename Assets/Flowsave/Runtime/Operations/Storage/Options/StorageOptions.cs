using System;

namespace Flowsave.Storage
{
    [Serializable]
    public class DefaultStorageOptions
    {
        public StorageType StorageType = StorageType.FileSystem;
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

                StorageType = from.StorageType,
                DiskStorage = DiskStorageOptions.Clone(from.DiskStorage),
                PlayerPrefsStorage = PlayerPrefsStorageOptions.Clone(from.PlayerPrefsStorage)
            };

        public static StorageOptions Clone(StorageOptions from) =>
            from == null ? null : new StorageOptions
            {
                UseDefault = from.UseDefault,

                StorageType = from.StorageType,
                DiskStorage = DiskStorageOptions.Clone(from.DiskStorage),
                PlayerPrefsStorage = PlayerPrefsStorageOptions.Clone(from.PlayerPrefsStorage)
            };

    }
}
