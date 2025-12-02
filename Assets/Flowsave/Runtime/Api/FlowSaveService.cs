using System;
using System.Text;
using System.Threading.Tasks;
using FlowSave.Configurations;
using FlowSave.Logging;
using FlowSave.Operations.Builder;
using FlowSave.Serialization;
using FlowSave.Storage;
using UnityEngine;

namespace FlowSave
{
    public sealed partial class FlowSaveService : IFlowSave
    {
        public static FlowSaveService Instance { get; private set; }

        private readonly FlowSaveConfiguration _config;

        public FlowSaveService(FlowSaveConfiguration config = default)
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

            var keyStore = _config.GetKeyStoreForEnvironment(env);

            var pipeline = FlowOperationPipeline<T>.CreateWritePipeline(env, keyStore, namespaceId);
            return await pipeline.ExecuteWriteAsync(data).ConfigureAwait(false);
        }

        public async Task<Result<T>> LoadAsync<T>(string namespaceId)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                return Result<T>.Failure("Namespace id is required.");

            var env = _config.GetEnvironmentConfiguration(namespaceId);
            if (env == null)
                return Result<T>.Failure($"No environment configuration found for namespace '{namespaceId}'.");

            var keyStore = _config.GetKeyStoreForEnvironment(env);

            var pipeline = FlowOperationPipeline<T>.CreateReadPipeline(env, keyStore, namespaceId);
            return await pipeline.ExecuteReadAsync().ConfigureAwait(false);
        }

        public async Task<Result<T[]>> LoadAllAsync<T>(string namespaceId)
        {
            if (string.IsNullOrWhiteSpace(namespaceId))
                return Result<T[]>.Failure("Namespace id is required.");

            var env = _config.GetEnvironmentConfiguration(namespaceId);
            if (env == null)
                return Result<T[]>.Failure($"No environment configuration found for namespace '{namespaceId}'.");

            if (!(env.StorageOptions?.DiskStorage?.Append ?? false))
                return Result<T[]>.Failure($"Namespace '{namespaceId}' is not configured for append mode.");

            var keyStore = _config.GetKeyStoreForEnvironment(env);

            // Build the same append read-all lambda we used above (you can share helper)
            var readAll = FlowOperationPipeline<T>.CreateAppendReadAll(env, keyStore, namespaceId);
            return await readAll().ConfigureAwait(false);
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

            var keyStore = _config.GetKeyStoreForEnvironment(env);

            var pipeline = FlowOperationPipeline<byte[]>.CreateWritePipeline(envClone, keyStore, namespaceId);
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

            var keyStore = _config.GetKeyStoreForEnvironment(env);

            var pipeline = FlowOperationPipeline<byte[]>.CreateReadPipeline(envClone, keyStore, namespaceId);
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
