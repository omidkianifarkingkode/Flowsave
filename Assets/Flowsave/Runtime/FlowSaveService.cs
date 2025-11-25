using System;
using System.Threading.Tasks;
using Flowsave.Namespaces;
using Flowsave.Operations;
using Flowsave.Storage;

namespace Flowsave
{
    public sealed class FlowSaveService : IFlowSaveService
    {
        private readonly FlowSaveConfiguration _config;
        private readonly IFileNameObfuscator _fileNameObfuscator;

        public FlowSaveService(
            FlowSaveConfiguration config,
            IFileNameObfuscator fileNameObfuscator = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileNameObfuscator = fileNameObfuscator ?? new Sha256FileNameObfuscator();
        }

        public async Task SaveAsync<T>(string namespaceId, T data)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                throw new ArgumentException("Namespace id is required.", nameof(namespaceId));

            var env = _config.GetEnvironmentConfiguration(namespaceId);

            var pipeline = FlowOperationPipeline<T>.CreateWritePipeline(
                env,
                logicalKey: namespaceId,
                obfuscator: _fileNameObfuscator);

            await pipeline.ExecuteWriteAsync(data).ConfigureAwait(false);
        }

        public async Task<T> LoadAsync<T>(string namespaceId)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                throw new ArgumentException("Namespace id is required.", nameof(namespaceId));

            var env = _config.GetEnvironmentConfiguration(namespaceId);

            var pipeline = FlowOperationPipeline<T>.CreateReadPipeline(
                env,
                logicalKey: namespaceId,
                obfuscator: _fileNameObfuscator);

            return await pipeline.ExecuteReadAsync().ConfigureAwait(false);
        }


        public Task<bool> HasSaveAsync(string namespaceId) =>
            throw new NotImplementedException();

        public Task DeleteSaveAsync(string namespaceId) =>
            throw new NotImplementedException();
    }
}
