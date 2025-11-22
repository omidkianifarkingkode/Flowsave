using Flowsave.Compression;
using Flowsave.Operations;
using Flowsave.Operations.Options;
using Flowsave.Serialization;
using Flowsave.Storage;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Flowsave.Namespaces
{
    public partial class FlowSaveConfiguration : ScriptableObject
    {
        public DefaultStorageOptions DefaultStorageOptions = new();
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
            #if !UNITY_EDITOR
            if (_cache.TryGetValue(namespaceId, out var cached))
                return cached;
            #endif

            var mode = GetCurrentMode();

            // 2. Start with pure default/global base
            var result = CreateBaseEnvironment(mode);

            // 3. Resolve global and namespace configurations
            var nsConfig = Namespaces?.FirstOrDefault(n => n.NamespaceId == namespaceId);
            var globalEnv = DefaultEnvironments?.FirstOrDefault(e => (e.AppMode & mode) == mode);
            var nsEnv = nsConfig?.Environments?.FirstOrDefault(e => (e.AppMode & mode) == mode);

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
            #if !UNITY_EDITOR
                _cache[namespaceId] = result;
            #endif

            return result;
        }

        private EnvironmentConfiguration CreateBaseEnvironment(AppMode mode)
        {
            return new EnvironmentConfiguration
            {
                AppMode = mode,
                SchemaVersion = 1,

                StorageOptions = StorageOptions.Clone(DefaultStorageOptions),
                Operations = new List<OperationMode>(),
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
            if (source.Operations != null && source.Operations.Count > 0)
            {
                if (target.Operations == null)
                    target.Operations = new List<OperationMode>();
                else
                    target.Operations.Clear();

                target.Operations.AddRange(source.Operations);
            }

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

        private static AppMode GetCurrentMode()
        {
#if UNITY_EDITOR
            return AppMode.Editor;
#elif DEVELOPMENT_BUILD
            return AppMode.Development;
#else
            AppMode.Release;
#endif
        }
    }
}
