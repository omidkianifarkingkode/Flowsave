using Flowsave.Compression;
using Flowsave.Security;
using Flowsave.Security.Options;
using Flowsave.Serialization;
using Flowsave.Shared;
using Flowsave.Storage;
using System;

namespace Flowsave.Configurations
{
    /// <summary>Full set of config fields.</summary>
    [Serializable]
    public class EnvironementFields
    {
        public AppMode AppMode;
        public OperationMode operations = OperationMode.None;

        public StorageOptions StorageOptions;
        public CompressionOptions CompressionOptions;
        public SerializationOptions SerializationOptions;
        public EncryptionOptions EncryptionOptions;
        public SigningOptions SigningOptions;

        public int schemaVersion = 1;
    }
}
