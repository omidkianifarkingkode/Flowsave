using Flowsave.Shared;

namespace Flowsave.Configurations
{
    /// <summary>Read-only runtime config.</summary>
    public interface IFlowSaveConfig
    {
        string NamespaceId { get; }
        SaveProviderType ProviderType { get; }
        SerializerType SerializerType { get; }
        SecurityOptions SecurityOptions { get; }
        string FilePath { get; } // RESOLVED absolute path
        bool EnableBackups { get; }
        int MaxBackups { get; }
        int SchemaVersion { get; }
        string EncryptionProfileId { get; }
    }
}
