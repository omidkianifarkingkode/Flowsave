using Flowsave.Compression;
using Flowsave.Security;
using Flowsave.Security.Options;
using Flowsave.Serialization;
using Flowsave.Storage;
using System;
using UnityEngine;

namespace Flowsave.Namespaces
{
    [Serializable]
    public class EnvironmentConfiguration
    {
        public EnvironmentMode Environment;
        public StorageOptions StorageOptions = new();

        public OperationMode Operations = OperationMode.None;
        public CompressionOptions CompressionOptions  = new();
        public SerializationOptions SerializationOptions = new();
        public EncryptionOptions EncryptionOptions  = new();
        public SigningOptions SigningOptions  = new();

        public int SchemaVersion = 1;

        public static EnvironmentConfiguration Clone(EnvironmentConfiguration from) =>
            from == null ? null : new EnvironmentConfiguration
            {
                Environment = from.Environment,
                SchemaVersion = from.SchemaVersion,
                Operations = from.Operations,

                StorageOptions = StorageOptions.Clone(from.StorageOptions),
                CompressionOptions = CompressionOptions.Clone(from.CompressionOptions),
                SerializationOptions = SerializationOptions.Clone(from.SerializationOptions),
                EncryptionOptions = EncryptionOptions.Clone(from.EncryptionOptions),
                SigningOptions = SigningOptions.Clone(from.SigningOptions)
            };

    }
}
