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
    public class NamespaceProperties
    {
        [field: SerializeField] public EnvironmentMode Environment { get; private set; }
        [field: SerializeField] public OperationMode Operations { get; private set; } = OperationMode.None;

        [field: SerializeField] public StorageOptions StorageOptions { get; private set; } = new();
        [field: SerializeField] public CompressionOptions CompressionOptions { get; private set; } = new();
        [field: SerializeField] public SerializationOptions SerializationOptions { get; private set; } = new();
        [field: SerializeField] public EncryptionOptions EncryptionOptions { get; private set; } = new();
        [field: SerializeField] public SigningOptions SigningOptions { get; private set; } = new();

        [field: SerializeField] public int SchemaVersion { get; private set; } = 1;
    }
}
