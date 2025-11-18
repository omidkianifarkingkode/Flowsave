using System;
using UnityEngine;

namespace Flowsave.Storage
{
    [Serializable]
    public class StorageOptions
    {
        [field: SerializeField] public StorageType StorageType { get; private set; } = StorageType.FileSystem;
        [field: SerializeField] public DiskStorageOptions DiskStorage { get; private set; } = new DiskStorageOptions();
        [field: SerializeField] public PlayerPrefsStorageOptions PlayerPrefsStorage { get; private set; } = new PlayerPrefsStorageOptions();
    }
}
