using System;

namespace FlowSave.Storage
{
    [Serializable]
    public class DiskStorageOptions
    {
        public StoragePathRoot PathRoot = StoragePathRoot.PersistentDataPath;

        // New template system
        public string PathTemplate = "saves/{NAMESPACE}.json";

        // When true, append instead of replacing
        public bool Append = false;

        // Keep rotating backups
        public bool KeepBackup = true;

        // Recommended >= 3
        public int MaxBackup = 3;

        public static DiskStorageOptions Clone(DiskStorageOptions from) =>
            from == null ? null : new DiskStorageOptions
            {
                PathRoot = from.PathRoot,
                PathTemplate = from.PathTemplate,
                Append = from.Append,
                KeepBackup = from.KeepBackup,
                MaxBackup = from.MaxBackup
            };
    }
}
