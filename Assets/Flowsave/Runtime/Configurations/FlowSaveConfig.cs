using System.Collections.Generic;
using UnityEngine;

namespace Flowsave.Configurations
{
    [CreateAssetMenu(fileName = "FlowSaveConfig", menuName = "FlowSave/Config", order = 1)]
    public class FlowSaveConfig : ScriptableObject
    {
        // The unique identifier for this namespace
        public string namespaceId;

        // Description for the namespace
        public string description;

        // Define the save provider (File, PlayerPrefs, Cloud, etc.)
        public SaveProviderType providerType;

        // Serializer type (Json, Binary, Custom, etc.)
        public SerializerType serializerType;

        // Encryption method used (None, AES, RSA, etc.)
        public SecurityMode securityMode;

        // File path for saving (relative to Application.persistentDataPath)
        public string filePath;

        // Whether to use backups
        public bool enableBackups;

        // Max number of backup versions to retain
        public int maxBackups;

        // Versioning: Set the schema version
        public int schemaVersion;

        // Allowed types for serialization (ensure type safety)
        public List<System.Type> allowedTypes;

        // Options for serializer (could be expanded based on serializer type)
        public JsonSerializerOptions jsonOptions;

        // Optional custom encryption settings
        public EncryptionSettings encryptionSettings;

        // Options for versioning and migration
        public List<SaveMigration> migrations;
    }
}