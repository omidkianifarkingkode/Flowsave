using System;
using System.Text;
using System.Threading.Tasks;
using Flowsave.Configurations;
using Flowsave.Operations.Builder;
using Flowsave.Serialization;
using Flowsave.Storage;
using UnityEngine;

#if FLOWSAVE_UNITASK
using Cysharp.Threading.Tasks;
#endif


namespace Flowsave
{
    public sealed partial class FlowSave : IFlowSave
    {
        public static FlowSave Instance { get; private set; }

        private readonly FlowSaveConfiguration _config;

        public FlowSave(FlowSaveConfiguration config = default)
        {
            if (config == null)
                config = Resources.Load<FlowSaveConfiguration>(nameof(FlowSaveConfiguration));

            _config = config != null ? config : throw new ArgumentNullException(nameof(config));

            Instance = this;
        }

        // ============================================================
        //  High-level (with serialization) – Task
        // ============================================================

        public async Task<Result> SaveAsync<T>(string namespaceId, T data)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                return Result.Failure("Namespace id is required.");

            var env = _config.GetEnvironmentConfiguration(namespaceId);
            if (env == null)
                return Result.Failure($"No environment configuration found for namespace '{namespaceId}'.");

            var pipeline = FlowOperationPipeline<T>.CreateWritePipeline(env, namespaceId);
            return await pipeline.ExecuteWriteAsync(data).ConfigureAwait(false);
        }

        public async Task<Result<T>> LoadAsync<T>(string namespaceId)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                return Result<T>.Failure("Namespace id is required.");

            var env = _config.GetEnvironmentConfiguration(namespaceId);
            if (env == null)
                return Result<T>.Failure($"No environment configuration found for namespace '{namespaceId}'.");

            var pipeline = FlowOperationPipeline<T>.CreateReadPipeline(env, namespaceId);
            return await pipeline.ExecuteReadAsync().ConfigureAwait(false);
        }

        public async Task<Result<bool>> HasSaveAsync(string namespaceId)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                return Result<bool>.Failure("Namespace id is required.");

            var env = _config.GetEnvironmentConfiguration(namespaceId);
            if (env == null)
                return Result<bool>.Failure($"No environment configuration found for namespace '{namespaceId}'.");

            var storageFactory = new StorageProviderFactory(env.StorageOptions);
            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            var existsResult = await storage.ExistsAsync(namespaceId).ConfigureAwait(false);
            if (!existsResult.IsSuccess)
                return Result<bool>.Failure(existsResult.Error);

            return Result<bool>.Success(existsResult.Value);
        }

        public async Task<Result> DeleteSaveAsync(string namespaceId)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                return Result.Failure("Namespace id is required.");

            var env = _config.GetEnvironmentConfiguration(namespaceId);
            if (env == null)
                return Result.Failure($"No environment configuration found for namespace '{namespaceId}'.");

            var storageFactory = new StorageProviderFactory(env.StorageOptions);
            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            var delResult = await storage.DeleteAsync(namespaceId).ConfigureAwait(false);
            return delResult;
        }

        // ============================================================
        //  Raw (no serialization) – Task
        // ============================================================

        public async Task<Result> SaveRawBytesAsync(string namespaceId, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                return Result.Failure("Namespace id is required.");
            if (data == null)
                return Result.Failure("Data is null.");

            var env = _config.GetEnvironmentConfiguration(namespaceId);
            if (env == null)
                return Result.Failure($"No environment configuration found for namespace '{namespaceId}'.");

            // Clone env and force no-serialization
            var envClone = EnvironmentConfiguration.Clone(env);
            envClone.SerializationOptions.SerializationType = SerializationType.None;

            var pipeline = FlowOperationPipeline<byte[]>.CreateWritePipeline(envClone, namespaceId);
            return await pipeline.ExecuteWriteAsync(data).ConfigureAwait(false);
        }

        public async Task<Result<byte[]>> LoadRawBytesAsync(string namespaceId)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                return Result<byte[]>.Failure("Namespace id is required.");

            var env = _config.GetEnvironmentConfiguration(namespaceId);
            if (env == null)
                return Result<byte[]>.Failure($"No environment configuration found for namespace '{namespaceId}'.");

            // Clone env and force no-serialization
            var envClone = EnvironmentConfiguration.Clone(env);
            envClone.SerializationOptions.SerializationType = SerializationType.None;

            var pipeline = FlowOperationPipeline<byte[]>.CreateReadPipeline(envClone, namespaceId);
            return await pipeline.ExecuteReadAsync().ConfigureAwait(false);
        }

        public async Task<Result> SaveRawStringAsync(string namespaceId, string text)
        {
            if (text == null)
                return Result.Failure("Text is null.");

            var bytes = Encoding.UTF8.GetBytes(text);
            return await SaveRawBytesAsync(namespaceId, bytes).ConfigureAwait(false);
        }

        public async Task<Result<string>> LoadRawStringAsync(string namespaceId)
        {
            var bytesResult = await LoadRawBytesAsync(namespaceId).ConfigureAwait(false);
            if (!bytesResult.IsSuccess)
                return Result<string>.Failure(bytesResult.Error);

            try
            {
                var text = Encoding.UTF8.GetString(bytesResult.Value);
                return Result<string>.Success(text);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"UTF8 decode failed: {ex.Message}");
            }
        }
    }
}
