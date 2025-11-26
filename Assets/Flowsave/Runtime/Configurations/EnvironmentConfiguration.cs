using Flowsave.Compression;
using Flowsave.Operations;
using Flowsave.Operations.Options;
using Flowsave.Serialization;
using Flowsave.Storage;
using System;
using System.Collections.Generic;

namespace Flowsave.Configurations
{
    [Serializable]
    public class EnvironmentConfiguration
    {
        //public string Name = "Environment"; //display name in editor

        public AppMode AppMode;
        public StorageOptions StorageOptions = new();

        public List<OperationMode> Operations = new();
        public CompressionOptions CompressionOptions = new();
        public SerializationOptions SerializationOptions = new();
        public EncryptionOptions EncryptionOptions = new();
        public SigningOptions SigningOptions = new();

        public int SchemaVersion = 1;

        public static EnvironmentConfiguration Clone(EnvironmentConfiguration from) =>
            from == null ? null : new EnvironmentConfiguration
            {
                AppMode = from.AppMode,
                SchemaVersion = from.SchemaVersion,

                Operations = from.Operations != null
                    ? new List<OperationMode>(from.Operations)
                    : new List<OperationMode>(),

                StorageOptions = StorageOptions.Clone(from.StorageOptions),
                CompressionOptions = CompressionOptions.Clone(from.CompressionOptions),
                SerializationOptions = SerializationOptions.Clone(from.SerializationOptions),
                EncryptionOptions = EncryptionOptions.Clone(from.EncryptionOptions),
                SigningOptions = SigningOptions.Clone(from.SigningOptions),
            };

    }
}
