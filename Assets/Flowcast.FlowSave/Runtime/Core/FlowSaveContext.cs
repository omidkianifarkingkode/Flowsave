using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flowcast.FlowSave
{
    /// <summary>
    /// Provides a high level API for persisting and loading data within a specific namespace.
    /// </summary>
    public class FlowSaveContext
    {
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly List<ISaveMigrator> _migrators;

        public FlowSaveContext(
            string name,
            ISaveProvider provider,
            ISerializer serializer,
            SaveVersion? version = null,
            IEncryptionService encryptionService = null,
            IEnumerable<ISaveMigrator> migrators = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Context name cannot be null or empty.", nameof(name));
            }

            Name = name;
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            EncryptionService = encryptionService;
            Version = version ?? new SaveVersion(1, 0, 0);

            _migrators = migrators?.OrderBy(m => m.FromVersion).ToList() ?? new List<ISaveMigrator>();
        }

        /// <summary>
        /// Gets the name/namespace of the context.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the serializer assigned to this context.
        /// </summary>
        public ISerializer Serializer { get; }

        /// <summary>
        /// Gets the provider assigned to this context.
        /// </summary>
        public ISaveProvider Provider { get; }

        /// <summary>
        /// Gets the encryption service assigned to this context.
        /// </summary>
        public IEncryptionService EncryptionService { get; }

        /// <summary>
        /// Gets the current version of the context.
        /// </summary>
        public SaveVersion Version { get; }

        /// <summary>
        /// Saves the provided data using the configured serializer and provider.
        /// </summary>
        public void Save<T>(string key, T data)
        {
            string storageKey = GetStorageKey(key);
            byte[] payload = Serializer.Serialize(data);
            PersistRawPayload(storageKey, payload);
            _cache[storageKey] = data;
        }

        /// <summary>
        /// Attempts to load a previously persisted payload.
        /// </summary>
        public bool TryLoad<T>(string key, out T data)
        {
            string storageKey = GetStorageKey(key);

            if (_cache.TryGetValue(storageKey, out object cached) && cached is T typed)
            {
                data = typed;
                return true;
            }

            if (!Provider.Exists(storageKey))
            {
                data = default;
                return false;
            }

            byte[] encrypted = Provider.Load(storageKey);
            byte[] envelope = DecryptIfNeeded(encrypted);
            var record = ExtractPayload(envelope);
            byte[] payload = EnsureCurrentVersion(storageKey, record.Version, record.Payload);

            data = Serializer.Deserialize<T>(payload);
            _cache[storageKey] = data;
            return true;
        }

        /// <summary>
        /// Loads the payload associated with the provided key.
        /// </summary>
        public T Load<T>(string key)
        {
            if (TryLoad(key, out T data))
            {
                return data;
            }

            throw new KeyNotFoundException($"No data stored for key '{key}' in context '{Name}'.");
        }

        /// <summary>
        /// Deletes the persisted payload associated with the provided key.
        /// </summary>
        public void Delete(string key)
        {
            string storageKey = GetStorageKey(key);
            if (Provider.Exists(storageKey))
            {
                Provider.Delete(storageKey);
            }

            _cache.Remove(storageKey);
        }

        /// <summary>
        /// Determines whether a payload exists for the provided key.
        /// </summary>
        public bool Exists(string key)
        {
            string storageKey = GetStorageKey(key);
            return Provider.Exists(storageKey);
        }

        /// <summary>
        /// Clears the in-memory cache for the context.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        private string GetStorageKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            return string.Concat(Name, ":", key);
        }

        private void PersistRawPayload(string storageKey, byte[] payload)
        {
            byte[] envelope = CreateEnvelope(payload);
            byte[] encrypted = EncryptIfNeeded(envelope);
            Provider.Save(storageKey, encrypted);
        }

        private byte[] EncryptIfNeeded(byte[] data)
        {
            return EncryptionService != null ? EncryptionService.Encrypt(data) : data;
        }

        private byte[] DecryptIfNeeded(byte[] data)
        {
            return EncryptionService != null ? EncryptionService.Decrypt(data) : data;
        }

        private byte[] CreateEnvelope(byte[] payload)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Version.Major);
                writer.Write(Version.Minor);
                writer.Write(Version.Patch);
                writer.Write(payload?.Length ?? 0);
                if (payload != null && payload.Length > 0)
                {
                    writer.Write(payload);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private (SaveVersion Version, byte[] Payload) ExtractPayload(byte[] envelope)
        {
            using (var stream = new MemoryStream(envelope))
            using (var reader = new BinaryReader(stream))
            {
                int major = reader.ReadInt32();
                int minor = reader.ReadInt32();
                int patch = reader.ReadInt32();
                int length = reader.ReadInt32();

                if (length < 0)
                {
                    throw new InvalidDataException("Invalid payload length detected while reading save data.");
                }

                byte[] payload = length > 0 ? reader.ReadBytes(length) : Array.Empty<byte>();
                return (new SaveVersion(major, minor, patch), payload);
            }
        }

        private byte[] EnsureCurrentVersion(string storageKey, SaveVersion storedVersion, byte[] payload)
        {
            if (storedVersion == Version)
            {
                return payload;
            }

            SaveVersion currentVersion = storedVersion;
            byte[] currentPayload = payload ?? Array.Empty<byte>();
            bool changed = false;

            foreach (ISaveMigrator migrator in _migrators)
            {
                if (migrator.FromVersion != currentVersion)
                {
                    continue;
                }

                currentPayload = migrator.Migrate(currentPayload) ?? Array.Empty<byte>();
                currentVersion = migrator.ToVersion;
                changed = true;

                if (currentVersion == Version)
                {
                    break;
                }
            }

            if (currentVersion != Version)
            {
                throw new InvalidOperationException($"Unable to migrate save data from version {storedVersion} to {Version} in context '{Name}'.");
            }

            if (changed)
            {
                PersistRawPayload(storageKey, currentPayload);
            }

            return currentPayload;
        }
    }
}
