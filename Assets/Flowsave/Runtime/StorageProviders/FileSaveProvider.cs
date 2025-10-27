using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace FlowSave
{
    public class FileStorageProvider : IStorageProvider
    {
        private readonly string basePath;

        public FileStorageProvider(string basePath = null)
        {
            this.basePath = basePath ?? Application.persistentDataPath;
        }

        public async Task SaveAsync(string key, byte[] data)
        {
            string filePath = GetFilePath(key);
            
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            
            await fileStream.WriteAsync(data, 0, data.Length);
        }

        public async Task<byte[]> LoadAsync(string key)
        {
            string filePath = GetFilePath(key);
            
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Data with key '{key}' does not exist.");
            }

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            
            byte[] data = new byte[fileStream.Length];
            await fileStream.ReadAsync(data, 0, data.Length);
            return data;
        }

        public async Task DeleteAsync(string key)
        {
            string filePath = GetFilePath(key);
            
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath)); // Asynchronous delete using Task.Run
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            string filePath = GetFilePath(key);
            
            return await Task.Run(() => File.Exists(filePath)); // Asynchronously check if file exists
        }

        private string GetFilePath(string key)
        {
            string fileName = key.Replace("/", "_").Replace("\\", "_"); // basic sanitization
            
            return Path.Combine(basePath, fileName + ".dat");
        }
    }
}
