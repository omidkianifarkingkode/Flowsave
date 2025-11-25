using System;
using System.Linq;
using System.Threading.Tasks;
using Flowsave.Compression;
using Flowsave.Namespaces;
using Flowsave.Operations;
using Flowsave.Serialization;
using Flowsave.Storage;

namespace Flowsave
{
    /// <summary>
    /// Unified read/write operation pipeline for FlowSave.
    /// 
    /// Write direction:
    ///     T -> serialize -> compress -> encrypt -> sign -> storage
    ///
    /// Read direction:
    ///     storage -> verify -> decrypt -> decompress -> deserialize -> T
    /// </summary>
    public sealed class FlowOperationPipeline<T>
    {
        private readonly Func<T, Task> _writePath;
        private readonly Func<Task<T>> _readPath;

        private FlowOperationPipeline(Func<T, Task> writePath, Func<Task<T>> readPath)
        {
            _writePath = writePath;
            _readPath = readPath;
        }

        /// <summary>
        /// Builds a full WRITE pipeline from the given environment:
        /// T -> serialize -> compress -> (encrypt) -> (sign) -> storage.
        /// </summary>
        public static FlowOperationPipeline<T> CreateWritePipeline(
            EnvironmentConfiguration env,
            string logicalKey,
            IFileNameObfuscator obfuscator = null)
        {
            if (env == null) throw new ArgumentNullException(nameof(env));
            if (string.IsNullOrWhiteSpace(logicalKey))
                throw new ArgumentException("Logical key is required.", nameof(logicalKey));

            // 1. Serializer (T -> byte[])
            var serializerFactory = new SerializerFactory(env.SerializationOptions);
            var serializer = serializerFactory.CreateSerializer(env.SerializationOptions.SerializationType);

            Func<T, Task<byte[]>> serialize = value =>
                Task.FromResult(serializer.Serialize(value));

            // 2. Storage provider (may obfuscate key internally)
            var storageFactory = new StorageProviderFactory(
                env.StorageOptions,
                env.UseFileNameObfuscation ? obfuscator : null);

            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            // 3. Build byte[] -> byte[] pipeline (compression, encryption, signing)
            var bytePipeline = BuildWriteBytesPipeline(env);

            // 4. Final write delegate
            async Task WriteAsync(T value)
            {
                // T -> bytes
                var bytes = await serialize(value).ConfigureAwait(false);

                // bytes -> processed bytes
                bytes = await bytePipeline(bytes).ConfigureAwait(false);

                // store
                await storage.SaveAsync(logicalKey, bytes).ConfigureAwait(false);
            }

            return new FlowOperationPipeline<T>(WriteAsync, readPath: null);
        }

        /// <summary>
        /// Builds a full READ pipeline from the given environment:
        /// storage -> (verify) -> (decrypt) -> decompress -> deserialize -> T.
        /// </summary>
        public static FlowOperationPipeline<T> CreateReadPipeline(
            EnvironmentConfiguration env,
            string logicalKey,
            IFileNameObfuscator obfuscator = null)
        {
            if (env == null) throw new ArgumentNullException(nameof(env));
            if (string.IsNullOrWhiteSpace(logicalKey))
                throw new ArgumentException("Logical key is required.", nameof(logicalKey));

            // 1. Serializer (byte[] -> T)
            var serializerFactory = new SerializerFactory(env.SerializationOptions);
            var serializer = serializerFactory.CreateSerializer(env.SerializationOptions.SerializationType);

            Func<byte[], Task<T>> deserialize = bytes =>
                Task.FromResult(serializer.Deserialize<T>(bytes));

            // 2. Storage provider (may obfuscate key internally)
            var storageFactory = new StorageProviderFactory(
                env.StorageOptions,
                env.UseFileNameObfuscation ? obfuscator : null);

            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            // 3. Build byte[] -> byte[] pipeline (verify, decrypt, decompress) – reverse order of write
            var bytePipeline = BuildReadBytesPipeline(env);

            // 4. Final read delegate
            async Task<T> ReadAsync()
            {
                // read raw stored bytes
                var bytes = await storage.LoadAsync(logicalKey).ConfigureAwait(false);

                // bytes -> processed bytes
                bytes = await bytePipeline(bytes).ConfigureAwait(false);

                // bytes -> T
                return await deserialize(bytes).ConfigureAwait(false);
            }

            return new FlowOperationPipeline<T>(writePath: null, readPath: ReadAsync);
        }

        // ------------------------------------------------------------
        //  PUBLIC EXECUTION
        // ------------------------------------------------------------

        /// <summary>
        /// Executes the WRITE pipeline (throws if this pipeline was created as READ).
        /// </summary>
        public Task ExecuteWriteAsync(T value)
        {
            if (_writePath == null)
                throw new InvalidOperationException("This FlowOperationPipeline was not created as a write pipeline.");
            return _writePath(value);
        }

        /// <summary>
        /// Executes the READ pipeline (throws if this pipeline was created as WRITE).
        /// </summary>
        public Task<T> ExecuteReadAsync()
        {
            if (_readPath == null)
                throw new InvalidOperationException("This FlowOperationPipeline was not created as a read pipeline.");
            return _readPath();
        }

        // ------------------------------------------------------------
        //  INTERNAL HELPERS: WRITE path (byte[] -> byte[])
        // ------------------------------------------------------------

        private static Func<byte[], Task<byte[]>> BuildWriteBytesPipeline(EnvironmentConfiguration env)
        {
            // Start as identity
            Func<byte[], Task<byte[]>> pipeline = bytes => Task.FromResult(bytes);

            // Compose helper
            static Func<byte[], Task<byte[]>> Chain(Func<byte[], Task<byte[]>> current, Func<byte[], Task<byte[]>> next)
            {
                if (current == null) return next;
                return async data => await next(await current(data).ConfigureAwait(false)).ConfigureAwait(false);
            }

            // 1) Compression
            bool compressionEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Compression) &&
                env.CompressionOptions != null &&
                env.CompressionOptions.CompressionType != CompressionType.None;

            ICompressor compressor;
            if (compressionEnabled)
            {
                var factory = new CompressorFactory();
                compressor = factory.CreateCompressor(env.CompressionOptions.CompressionType);
            }
            else
            {
                compressor = new NoOpCompressor();
            }

            pipeline = Chain(pipeline, bytes => Task.FromResult(compressor.Compress(bytes)));

            // 2) Encryption (TODO: real implementation)
            bool encryptionEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Encrypt) &&
                env.EncryptionOptions != null &&
                env.EncryptionOptions.EncryptionType != EncryptionType.None;

            if (encryptionEnabled)
            {
                // TODO: create IEncryptor via EncryptorFactory and wrap in an envelope
                pipeline = Chain(pipeline, bytes => Task.FromResult(bytes)); // placeholder
            }

            // 3) Signing (TODO: real implementation)
            bool signingEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Sign) &&
                env.SigningOptions != null &&
                env.SigningOptions.SigningType != SigningType.None;

            if (signingEnabled)
            {
                // TODO: create ISigner via SignerFactory and attach signature to envelope
                pipeline = Chain(pipeline, bytes => Task.FromResult(bytes)); // placeholder
            }

            return pipeline;
        }

        // ------------------------------------------------------------
        //  INTERNAL HELPERS: READ path (byte[] -> byte[])
        // ------------------------------------------------------------

        private static Func<byte[], Task<byte[]>> BuildReadBytesPipeline(EnvironmentConfiguration env)
        {
            // Start as identity
            Func<byte[], Task<byte[]>> pipeline = bytes => Task.FromResult(bytes);

            // Compose helper
            static Func<byte[], Task<byte[]>> Chain(Func<byte[], Task<byte[]>> current, Func<byte[], Task<byte[]>> next)
            {
                if (current == null) return next;
                return async data => await next(await current(data).ConfigureAwait(false)).ConfigureAwait(false);
            }

            // IMPORTANT: reverse order compared to write:
            //  1) Verify signature
            //  2) Decrypt
            //  3) Decompress

            bool signingEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Sign) &&
                env.SigningOptions != null &&
                env.SigningOptions.SigningType != SigningType.None;

            if (signingEnabled)
            {
                // TODO: create ISigner via SignerFactory and verify signature from envelope
                pipeline = Chain(pipeline, bytes => Task.FromResult(bytes)); // placeholder
            }

            bool encryptionEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Encrypt) &&
                env.EncryptionOptions != null &&
                env.EncryptionOptions.EncryptionType != EncryptionType.None;

            if (encryptionEnabled)
            {
                // TODO: create IEncryptor via EncryptorFactory and decrypt envelope
                pipeline = Chain(pipeline, bytes => Task.FromResult(bytes)); // placeholder
            }

            bool compressionEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Compression) &&
                env.CompressionOptions != null &&
                env.CompressionOptions.CompressionType != CompressionType.None;

            ICompressor compressor;
            if (compressionEnabled)
            {
                var factory = new CompressorFactory();
                compressor = factory.CreateCompressor(env.CompressionOptions.CompressionType);
            }
            else
            {
                compressor = new NoOpCompressor();
            }

            pipeline = Chain(pipeline, bytes => Task.FromResult(compressor.Decompress(bytes)));

            return pipeline;
        }
    }
}
