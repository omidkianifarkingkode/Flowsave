using Flowsave.Compression;
using Flowsave.Security;
using Flowsave.Security.Options;
using Flowsave.Serialization;
using Flowsave.Storage;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Flowsave.Namespaces
{
    [CreateAssetMenu(fileName = "FlowSaveConfiguration", menuName = "FlowSave/Config Repository", order = 2)]
    public class FlowSaveConfiguration : ScriptableObject
    {
        public DefaultStorageOptions DefaultStorageOptions = new();
        public OperationMode DefaultOperations = OperationMode.None;
        public DefaultCompressionOptions DefaultCompressionOptions = new();
        public DefaultSerializationOptions DefaultSerializationOptions = new();
        public DefaultEncryptionOptions DefaultEncryptionOptions = new();
        public DefaultSigningOptions DefaultSigningOptions = new();
        [Space()]
        public List<EnvironmentConfiguration> DefaultEnvironments;
        public List<NamespaceConfiguration> Namespaces;

        private readonly Dictionary<string, EnvironmentConfiguration> _cache = new();

        public EnvironmentConfiguration GetEnvironmentConfiguration(string namespaceId)
        {
            // 1. Cached?
           // if (_cache.TryGetValue(namespaceId, out var cached))
           //     return cached;

            var mode = GetCurrentMode();

            // 2. Start with pure default/global base
            var result = CreateBaseEnvironment(mode);

            // 3. Resolve global and namespace configurations
            var nsConfig = Namespaces?.FirstOrDefault(n => n.NamespaceId == namespaceId);
            var globalEnv = DefaultEnvironments?.FirstOrDefault(e => (e.Environment & mode) == mode);
            var nsEnv = nsConfig?.Environments?.FirstOrDefault(e => (e.Environment & mode) == mode);

            // 4. Your flow
            if (nsEnv != null)
            {
                // Namespace > Global > Defaults
                ApplyEnvConfig(result, globalEnv);
                ApplyEnvConfig(result, nsEnv);
            }
            else if (globalEnv != null)
            {
                // Global > Defaults
                ApplyEnvConfig(result, globalEnv);
            }
            // else: only default base remains

            // 5. Cache the final result
            _cache[namespaceId] = result;

            return result;
        }

        private EnvironmentConfiguration CreateBaseEnvironment(EnvironmentMode mode)
        {
            return new EnvironmentConfiguration
            {
                Environment = mode,
                SchemaVersion = 1,

                StorageOptions = StorageOptions.Clone(DefaultStorageOptions),
                Operations = DefaultOperations,
                CompressionOptions = CompressionOptions.Clone(DefaultCompressionOptions),
                SerializationOptions = SerializationOptions.Clone(DefaultSerializationOptions),
                EncryptionOptions = EncryptionOptions.Clone(DefaultEncryptionOptions),
                SigningOptions = SigningOptions.Clone(DefaultSigningOptions)
            };
        }

        private void ApplyEnvConfig(EnvironmentConfiguration target, EnvironmentConfiguration source)
        {
            if (source == null) return;

            // Storage
            if (!source.StorageOptions.UseDefault)
                target.StorageOptions = StorageOptions.Clone(source.StorageOptions);

            // Ops
            if (source.Operations != OperationMode.None)
                target.Operations = source.Operations;

            // Compression
            if (!source.CompressionOptions.UseDefault)
                target.CompressionOptions = CompressionOptions.Clone(source.CompressionOptions);

            // Serialization
            if (!source.SerializationOptions.UseDefault)
                target.SerializationOptions = SerializationOptions.Clone(source.SerializationOptions);

            // Encryption
            if (!source.EncryptionOptions.UseDefault)
                target.EncryptionOptions = EncryptionOptions.Clone(source.EncryptionOptions);

            // Signing
            if (!source.SigningOptions.UseDefault)
                target.SigningOptions = SigningOptions.Clone(source.SigningOptions);

            // Schema
            if (source.SchemaVersion != 1)
                target.SchemaVersion = source.SchemaVersion;
        }

        private static EnvironmentMode GetCurrentMode()
        {
#if UNITY_EDITOR
            return EnvironmentMode.Editor;
#elif DEVELOPMENT_BUILD
            return EnvironmentMode.Development;
#else
            EnvironmentMode.Release;
#endif
        }

        [ContextMenu("Print")]
        public void Print() 
        {
            var enConfig = GetEnvironmentConfiguration("test");

            Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(enConfig, Newtonsoft.Json.Formatting.Indented));
        }

        [ContextMenu("AddNamespace")]
        public void AddNamespace() 
        {
            var envConfig = EnvironmentConfiguration.Clone(DefaultEnvironments.First());

            var newNs = new NamespaceConfiguration() 
            {
                NamespaceId = "test",
                Environments = new List<EnvironmentConfiguration> { envConfig }
            };

            Namespaces.Add(newNs);

            UnityEditor.EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
