using System;
using UnityEngine;

namespace Flowsave.Storage
{
    [Serializable]
    public class DiskStorageOptions
    {
        public StoragePathRoot PathRoot = StoragePathRoot.PersistentDataPath;
        public string RelativeDirectory = "saves/{NAMESPACE}.json";
        public string FileExtension = ".json";
        public bool Append = false;
        public bool KeepBackup = true;
        public int MaxBackup  = 3;

        public static DiskStorageOptions Clone(DiskStorageOptions from) =>
            from == null ? null : new DiskStorageOptions
            {
                PathRoot = from.PathRoot,
                RelativeDirectory = from.RelativeDirectory,
                FileExtension = from.FileExtension,
                Append = from.Append,
                KeepBackup = from.KeepBackup,
                MaxBackup = from.MaxBackup
            };

    }
}
