using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Flowsave.StorageProviders
{
    public class PlayerPrefsStorageProvider : IStorageProvider
    {
        public async Task SaveAsync(string key, byte[] data)
        {
            string base64Data = Convert.ToBase64String(data);
            await Task.Run(() => PlayerPrefs.SetString(key, base64Data)); // Async operation in Task
            PlayerPrefs.Save(); // PlayerPrefs is synchronous, but we can run it in Task
        }

        public async Task<byte[]> LoadAsync(string key)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                throw new InvalidOperationException($"Data with key '{key}' does not exist.");
            }

            string base64Data = PlayerPrefs.GetString(key);
            return await Task.FromResult(Convert.FromBase64String(base64Data)); // Simulate async return
        }

        public async Task DeleteAsync(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                await Task.Run(() => PlayerPrefs.DeleteKey(key)); // Async delete wrapped in Task
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await Task.FromResult(PlayerPrefs.HasKey(key)); // Return async result
        }
    }
}
