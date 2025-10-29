// File: Flowsave/Configurations/FlowSaveConfigFields.cs
using Flowsave.Shared;
using System;

namespace Flowsave.Configurations
{
    /// <summary>Full set of config fields.</summary>
    [Serializable]
    public class FlowSaveConfigFields
    {
        public SaveProviderType providerType = SaveProviderType.FileSystem;
        public SerializerType serializerType = SerializerType.Json;

        // FLAGS instead of single mode
        public SecurityOptions securityOptions = SecurityOptions.None;

        // Path root + path (relative if root != Absolute)
        public StoragePathRoot pathRoot = StoragePathRoot.PersistentDataPath;
        public string path = "saves/{NAMESPACE}.json";

        public bool enableBackups = true;
        public int maxBackups = 3;

        public int schemaVersion = 1;

        public string encryptionProfileId = ""; // resolve keys via a profile provider
    }
}
