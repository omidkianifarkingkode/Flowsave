using Flowsave.Shared;

namespace Flowsave.Configurations
{
    /// <summary>Immutable view implementing IFlowSaveConfig for the chosen environment.</summary>
    public sealed class FlowSaveConfigSnapshot : IFlowSaveConfig
    {
        public string NamespaceId { get; }
        public SaveProviderType ProviderType { get; }
        public SerializerType SerializerType { get; }
        public SecurityOptions SecurityOptions { get; }
        public string FilePath { get; } // RESOLVED absolute path
        public bool EnableBackups { get; }
        public int MaxBackups { get; }
        public int SchemaVersion { get; }
        public string EncryptionProfileId { get; }

        public FlowSaveConfigSnapshot(string ns, FlowSaveConfigFields f)
        {
            NamespaceId = ns ?? "";
            ProviderType = f.providerType;
            SerializerType = f.serializerType;
            SecurityOptions = f.securityOptions;
            FilePath = PathResolver.Resolve(f.pathRoot, (f.path ?? "").Replace("{NAMESPACE}", NamespaceId));
            EnableBackups = f.enableBackups;
            MaxBackups = f.maxBackups;
            SchemaVersion = f.schemaVersion;
            EncryptionProfileId = f.encryptionProfileId ?? "";
        }
    }
}
