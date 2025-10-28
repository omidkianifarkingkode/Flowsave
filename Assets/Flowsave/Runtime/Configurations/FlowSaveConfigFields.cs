// File: Flowsave/Configurations/FlowSaveConfigFields.cs
using Flowsave.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

    [Serializable]
    public class EnvironmentConfig
    {
        public AppMode mode;
        public FlowSaveConfigFields fields = new();
    }

    public static class PathResolver
    {
        public static string Resolve(StoragePathRoot root, string subPath)
        {
            string basePath = root switch
            {
                StoragePathRoot.ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? "",
                StoragePathRoot.PersistentDataPath => Application.persistentDataPath,
                StoragePathRoot.DataPath => Application.dataPath,
                StoragePathRoot.TemporaryCachePath => Application.temporaryCachePath,
                StoragePathRoot.Absolute => "",
                _ => ""
            };

            if (root == StoragePathRoot.Absolute)
                return subPath ?? "";

            if (string.IsNullOrEmpty(basePath))
                return subPath ?? "";

            if (string.IsNullOrEmpty(subPath))
                return basePath;

            return Path.GetFullPath(Path.Combine(basePath, subPath));
        }
    }

    /// <summary>Per-mode override flags + values (no generics for Unity serialization).</summary>
    [Serializable]
    public class PartialFlowSaveConfigFields
    {
        public bool overrideProviderType; public SaveProviderType providerType;
        public bool overrideSerializerType; public SerializerType serializerType;
        public bool overrideSecurityMode; public SecurityOptions securityMode;

        public bool overrideFilePath; public string filePath;
        public bool overrideEnableBackups; public bool enableBackups;
        public bool overrideMaxBackups; public int maxBackups;

        public bool overrideSchemaVersion; public int schemaVersion;

        public bool overrideEncryptionProfileId; public string encryptionProfileId;

        public bool overrideAllowedTypeNames; public List<string> allowedTypeNames = new();

        public bool HasAnyOverride()
        {
            return overrideProviderType || overrideSerializerType || overrideSecurityMode ||
                   overrideFilePath || overrideEnableBackups || overrideMaxBackups ||
                   overrideSchemaVersion || overrideEncryptionProfileId || overrideAllowedTypeNames;
        }
    }

    /// <summary>One row of overrides for a specific mode.</summary>
    [Serializable]
    public class ModeOverride
    {
        public AppMode mode;
        public PartialFlowSaveConfigFields fields = new();
    }
}
