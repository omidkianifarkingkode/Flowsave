using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Flowsave.StorageProviders
{
    /// <summary>
    /// PlayerPrefs-backed storage provider. Stores binary payloads as Base64 strings,
    /// chunked across multiple PlayerPrefs keys to avoid size issues.
    /// NOTE: PlayerPrefs is not designed for large blobs. Prefer small payloads (a few KBs).
    /// </summary>
    public sealed class PlayerPrefsStorageProvider : IStorageProvider
    {
        private readonly string _prefix;
        private readonly int _chunkChars;
        private readonly bool _autoSave;

        /// <param name="prefix">Prefix added to all PlayerPrefs keys (namespacing).</param>
        /// <param name="chunkChars">Chunk size in characters for Base64 data. 16k is a conservative default.</param>
        /// <param name="autoSave">If true, calls PlayerPrefs.Save() after each mutation.</param>
        public PlayerPrefsStorageProvider(string prefix = "fs:", int chunkChars = 16_384, bool autoSave = true)
        {
            if (string.IsNullOrEmpty(prefix)) prefix = "fs:";
            _prefix = prefix;
            _chunkChars = Math.Max(1024, chunkChars);
            _autoSave = autoSave;
        }

        public Task SaveAsync(string key, byte[] data)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (data == null) throw new ArgumentNullException(nameof(data));

            string b64 = Convert.ToBase64String(data);
            int newCount = (int)Math.Ceiling((double)b64.Length / _chunkChars);
            if (newCount == 0) newCount = 1; // store at least 1 chunk (empty string)

            int oldCount = GetCount(key);

            // write chunks
            for (int i = 0; i < newCount; i++)
            {
                int start = i * _chunkChars;
                int len = Math.Min(_chunkChars, b64.Length - start);
                string chunk = len > 0 ? b64.Substring(start, len) : string.Empty;
                PlayerPrefs.SetString(ChunkKey(key, i), chunk);
            }

            // remove any leftover old chunks
            for (int i = newCount; i < oldCount; i++)
            {
                PlayerPrefs.DeleteKey(ChunkKey(key, i));
            }

            // set count last
            PlayerPrefs.SetInt(CountKey(key), newCount);

            if (_autoSave) PlayerPrefs.Save();
            return Task.CompletedTask;
        }

        public Task<byte[]> LoadAsync(string key)
        {
            if (!HasKey(key)) throw new InvalidOperationException($"Key not found: {key}");

            int count = GetCount(key);
            if (count <= 0)
            {
                // legacy single-key fallback
                if (PlayerPrefs.HasKey(LegacyKey(key)))
                {
                    string s = PlayerPrefs.GetString(LegacyKey(key), string.Empty);
                    return Task.FromResult(Convert.FromBase64String(s));
                }
                throw new InvalidOperationException($"Corrupt PlayerPrefs entry for key: {key}");
            }

            var sb = new StringBuilder(count * _chunkChars);
            for (int i = 0; i < count; i++)
            {
                sb.Append(PlayerPrefs.GetString(ChunkKey(key, i), string.Empty));
            }

            string b64 = sb.ToString();
            byte[] data = Convert.FromBase64String(b64);
            return Task.FromResult(data);
        }

        public Task DeleteAsync(string key)
        {
            if (!HasKey(key)) return Task.CompletedTask;

            int count = GetCount(key);
            for (int i = 0; i < count; i++)
            {
                PlayerPrefs.DeleteKey(ChunkKey(key, i));
            }
            PlayerPrefs.DeleteKey(CountKey(key));
            PlayerPrefs.DeleteKey(LegacyKey(key)); // cleanup legacy too

            if (_autoSave) PlayerPrefs.Save();
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key) => Task.FromResult(HasKey(key));

        // ===== Helpers =====
        private string CountKey(string key) => _prefix + key + "__count";
        private string ChunkKey(string key, int i) => _prefix + key + "__" + i.ToString();
        private string LegacyKey(string key) => _prefix + key; // old single-string storage

        private int GetCount(string key) => PlayerPrefs.GetInt(CountKey(key), 0);
        private bool HasKey(string key) => PlayerPrefs.HasKey(CountKey(key)) || PlayerPrefs.HasKey(LegacyKey(key));
    }
}
