using Flowsave.KeyStorage;
using FlowSave.Compression;
using FlowSave.Encryption;
using FlowSave.KeyStorage;
using FlowSave.Logging;
using FlowSave.Operations;
using FlowSave.Serialization;
using FlowSave.Signing;
using FlowSave.Storage;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlowSave.Configurations
{
    public partial class FlowSaveConfiguration : ScriptableObject
    {
        public DefaultStorageOptions DefaultStorageOptions = new();
        public DefaultCompressionOptions DefaultCompressionOptions = new();
        public DefaultSerializationOptions DefaultSerializationOptions = new();
        public DefaultEncryptionOptions DefaultEncryptionOptions = new();
        public DefaultSigningOptions DefaultSigningOptions = new();
        public LoggingOptions LoggingOptions = new();
        [Space()]
        public List<EnvironmentConfiguration> DefaultEnvironments;
        public List<NamespaceConfiguration> Namespaces;
        public List<AppModeKeyStore> KeyStores = new();

        /// <summary>
        /// Optional delegate to override the runtime AppMode resolution.
        /// If null, compile-time symbols (UNITY_EDITOR / DEVELOPMENT_BUILD) are used.
        /// </summary>
        public static System.Func<AppMode> ModeResolver { get; set; }

        private readonly Dictionary<string, EnvironmentConfiguration> _cache = new();

        public EnvironmentConfiguration GetEnvironmentConfiguration(string namespaceId)
        {
            if (string.IsNullOrEmpty(namespaceId))
                namespaceId = string.Empty;

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

            // 4. Apply in correct precedence:
            //    Defaults -> Global -> Namespace
            if (nsEnv != null)
            {
                if (globalEnv != null)
                    ApplyEnvConfig(result, globalEnv);

                ApplyEnvConfig(result, nsEnv);
            }
            else if (globalEnv != null)
            {
                ApplyEnvConfig(result, globalEnv);
            }
            // else: only Defaults

            // 5. Cache final resolved env per namespace (builds only)
#if !UNITY_EDITOR
                _cache[namespaceId] = result;
#endif

            return result;
        }

        public KeyStoreOptions GetKeyStoreForMode(AppMode mode)
        {
            if (KeyStores == null || KeyStores.Count == 0)
                return null;

            if (mode == AppMode.None)
                return null;

            int modeMask = (int)mode;
            AppModeKeyStore bestExact = null;
            AppModeKeyStore bestOverlap = null;

            foreach (var store in KeyStores)
            {
                if (store == null || store.KeyStore == null)
                    continue;

                var storeMode = store.AppMode;
                if (storeMode == AppMode.None)
                    continue;

                int storeMask = (int)storeMode;

                // no overlap
                if ((storeMask & modeMask) == 0)
                    continue;

                // exact match → best
                if (storeMask == modeMask)
                {
                    bestExact = store;
                    break;
                }

                // first overlapping one as fallback
                if (bestOverlap == null)
                    bestOverlap = store;
            }

            return (bestExact ?? bestOverlap)?.KeyStore;
        }

        public KeyStoreOptions GetKeyStoreForEnvironment(EnvironmentConfiguration env)
        {
            if (env == null)
                return null;

            return GetKeyStoreForMode(env.AppMode);
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
            // Runtime override (used by FlowSaveBootstrapper)
            if (ModeResolver != null)
                return ModeResolver();

#if UNITY_EDITOR
            return AppMode.Editor;
#elif DEVELOPMENT_BUILD
            return AppMode.Development;
#else
            return AppMode.Release;
#endif
        }
    }
}
