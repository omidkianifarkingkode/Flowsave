using System;
using UnityEngine;

namespace Flowsave.Storage
{
    [Serializable]
    public class DiskStorageOptions
    {
        [field: SerializeField] public StoragePathRoot PathRoot { get; private set; } = StoragePathRoot.PersistentDataPath;
        [field:SerializeField] public string RelativeDirectory { get; private set; } = "saves/{NAMESPACE}.json";
        [field:SerializeField] public string FileExtension { get; private set; } = ".json";
        [field: SerializeField] public bool KeepBackup { get; private set; } = true;
        [field: SerializeField] public int MaxBackup { get; private set; } = 3;
    }
}
