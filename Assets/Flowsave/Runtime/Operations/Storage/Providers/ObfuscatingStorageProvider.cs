using Flowsave.Operations;
using System.Threading.Tasks;

namespace Flowsave.Storage
{
    public sealed class ObfuscatingStorageProvider : IStorageProvider
    {
        private readonly IStorageProvider _inner;
        private readonly IFileNameObfuscator _obfuscator;

        public ObfuscatingStorageProvider(IStorageProvider inner, IFileNameObfuscator obfuscator)
        {
            _inner = inner;
            _obfuscator = obfuscator;
        }

        private string Obf(string key) => _obfuscator.ObfuscateFilename(key);

        public Task<Result> SaveAsync(string key, byte[] data) =>
            _inner.SaveAsync(Obf(key), data);

        public Task<Result<byte[]>> LoadAsync(string key) =>
            _inner.LoadAsync(Obf(key));

        public Task<Result> DeleteAsync(string key) =>
            _inner.DeleteAsync(Obf(key));

        public Task<Result<bool>> ExistsAsync(string key) =>
            _inner.ExistsAsync(Obf(key));
    }
}
