using System.Collections.Generic;

namespace Flowsave.Configurations
{
    public static class DefaultFlowSaveConfig
    {
        // This method will provide the default configuration
        public static FlowSaveConfig GetDefaultConfig(string namespaceId)
        {
            return new FlowSaveConfig
            {
                namespaceId = namespaceId,
                description = $"Default configuration for {namespaceId}",
                serializerType = SerializerType.Json, // Default to JSON serializer
                providerType = SaveProviderType.FileSystem, // Default to FileSystem storage
                securityMode = SecurityMode.None, // Default to no security
                filePath = $"saves/{namespaceId}.json", // Default file path
                enableBackups = true, // Enable backups
                maxBackups = 3, // Retain 3 backups
                schemaVersion = 1, // Initial schema version
                allowedTypes = new List<System.Type>(), // Empty allowed types list
            };
        }
    }
}