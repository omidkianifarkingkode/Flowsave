using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FlowSave.Storage
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

        public PlayerPrefsStorageProvider(string prefix = "fs:", int chunkChars = 16_384, bool autoSave = true)
        {
            if (string.IsNullOrEmpty(prefix)) prefix = "fs:";
            _prefix = prefix;
            _chunkChars = Math.Max(1024, chunkChars);
            _autoSave = autoSave;
        }

        public PlayerPrefsStorageProvider(PlayerPrefsStorageOptions options)
            : this(options.Prefix, options.ChunkChars, options.AutoSave) { }

        public Task<Result> SaveAsync(string key, byte[] data)
        {
            if (key == null)
                return Result.Failure("Key is null.").ToTask();
            if (data == null)
                return Result.Failure("Data is null.").ToTask();

            try
            {
                string b64 = Convert.ToBase64String(data);
                int newCount = (int)Math.Ceiling((double)b64.Length / _chunkChars);
                if (newCount == 0) newCount = 1;

                int oldCount = GetCount(key);

                for (int i = 0; i < newCount; i++)
                {
                    int start = i * _chunkChars;
                    int len = Math.Min(_chunkChars, b64.Length - start);
                    string chunk = len > 0 ? b64.Substring(start, len) : string.Empty;
                    PlayerPrefs.SetString(ChunkKey(key, i), chunk);
                }

                for (int i = newCount; i < oldCount; i++)
                    PlayerPrefs.DeleteKey(ChunkKey(key, i));

                PlayerPrefs.SetInt(CountKey(key), newCount);

                if (_autoSave) PlayerPrefs.Save();

                return Result.Success().ToTask();
            }
            catch (Exception ex)
            {
                return Result.Failure($"PlayerPrefs save failed: {ex.Message}").ToTask();
            }
        }

        public Task<Result<byte[]>> LoadAsync(string key)
        {
            if (key == null)
                return Result<byte[]>.Failure("Key is null.").ToTask();

            try
            {
                if (!HasKey(key))
                    return Result<byte[]>.Failure($"Key not found: {key}").ToTask();

                int count = GetCount(key);
                if (count <= 0)
                {
                    if (PlayerPrefs.HasKey(LegacyKey(key)))
                    {
                        string s = PlayerPrefs.GetString(LegacyKey(key), string.Empty);
                        byte[] legacy = Convert.FromBase64String(s);
                        return Result<byte[]>.Success(legacy).ToTask();
                    }

                    return Result<byte[]>.Failure($"Corrupt PlayerPrefs entry for key: {key}").ToTask();
                }

                var sb = new StringBuilder(count * _chunkChars);
                for (int i = 0; i < count; i++)
                    sb.Append(PlayerPrefs.GetString(ChunkKey(key, i), string.Empty));

                string b64 = sb.ToString();
                byte[] data = Convert.FromBase64String(b64);

                return Result<byte[]>.Success(data).ToTask();
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"PlayerPrefs load failed: {ex.Message}").ToTask();
            }
        }

        public Task<Result> DeleteAsync(string key)
        {
            if (key == null)
                return Result.Failure("Key is null.").ToTask();

            try
            {
                if (!HasKey(key))
                    return Result.Success().ToTask(); // nothing to delete

                int count = GetCount(key);
                for (int i = 0; i < count; i++)
                    PlayerPrefs.DeleteKey(ChunkKey(key, i));

                PlayerPrefs.DeleteKey(CountKey(key));
                PlayerPrefs.DeleteKey(LegacyKey(key));

                if (_autoSave) PlayerPrefs.Save();

                return Result.Success().ToTask();
            }
            catch (Exception ex)
            {
                return Result.Failure($"PlayerPrefs delete failed: {ex.Message}").ToTask();
            }
        }

        public Task<Result<bool>> ExistsAsync(string key)
        {
            if (key == null)
                return Result<bool>.Failure("Key is null.").ToTask();

            try
            {
                bool exists = HasKey(key);
                return Result<bool>.Success(exists).ToTask();
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"PlayerPrefs exists check failed: {ex.Message}").ToTask();
            }
        }

        // Helpers
        private string CountKey(string key) => _prefix + key + "__count";
        private string ChunkKey(string key, int i) => _prefix + key + "__" + i.ToString();
        private string LegacyKey(string key) => _prefix + key;

        private int GetCount(string key) => PlayerPrefs.GetInt(CountKey(key), 0);

        private bool HasKey(string key) =>
            PlayerPrefs.HasKey(CountKey(key)) || PlayerPrefs.HasKey(LegacyKey(key));
    }
}
