using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Flowsave.StorageProviders
{
    /// <summary>
    /// Async in-memory storage provider for tests.
    /// </summary>
    public sealed class MemoryStorageProvider : IStorageProvider
    {
        private readonly ConcurrentDictionary<string, byte[]> _mem = new();

        public Task SaveAsync(string key, byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _mem[key] = (byte[])data.Clone();
            return Task.CompletedTask;
        }

        public Task<byte[]> LoadAsync(string key)
        {
            if (!_mem.TryGetValue(key, out var buf))
                throw new InvalidOperationException($"Key not found: {key}");
            return Task.FromResult((byte[])buf.Clone());
        }

        public Task DeleteAsync(string key)
        {
            _mem.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key) => Task.FromResult(_mem.ContainsKey(key));
    }
}
