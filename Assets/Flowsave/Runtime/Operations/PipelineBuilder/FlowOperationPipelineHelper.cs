using FlowSave.Compression;
using FlowSave.Configurations;
using FlowSave.Encryption;
using FlowSave.Serialization;
using FlowSave.Signing;
using FlowSave.Storage;
using System;
using System.Threading.Tasks;

namespace FlowSave.Operations.Builder
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
    public sealed partial class FlowOperationPipeline<T>
    {
        private delegate Task<Result<byte[]>> ByteStep(byte[] input);

        // ------------------------------------------------------------
        //  FACTORIES
        // ------------------------------------------------------------

        /// <summary>
        /// Builds a full WRITE pipeline from the given environment:
        /// T -> serialize -> compress -> (encrypt) -> (sign) -> storage.
        /// </summary>
        public static FlowOperationPipeline<T> CreateWritePipeline(EnvironmentConfiguration env, string logicalKey)
        {
            if (env == null)
                throw new ArgumentNullException(nameof(env));

            if (string.IsNullOrWhiteSpace(logicalKey))
                throw new ArgumentException("Logical key is required.", nameof(logicalKey));

            // 1. Serializer (T -> Result<byte[]>)
            var serializerFactory = new SerializerFactory(env.SerializationOptions);
            var serializer = serializerFactory.CreateSerializer(env.SerializationOptions.SerializationType);

            Result<byte[]> Serialize(T value) => serializer.Serialize(value);

            // 2. Storage provider (may obfuscate key internally)
            var storageFactory = new StorageProviderFactory(env.StorageOptions);
            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            // 3. Build byte[] -> Result<byte[]> pipeline (compression, encryption, signing)
            var bytePipeline = BuildWriteBytesPipeline(env);

            // 4. Final write delegate
            async Task<Result> WriteAsync(T value)
            {
                // T -> bytes
                var serResult = Serialize(value);
                if (!serResult.IsSuccess)
                    return Result.Failure(serResult.Error);

                var bytes = serResult.Value;

                // bytes -> processed bytes
                var pipeResult = await bytePipeline(bytes).ConfigureAwait(false);
                if (!pipeResult.IsSuccess)
                    return Result.Failure(pipeResult.Error);

                // store
                var storeResult = await storage.SaveAsync(logicalKey, pipeResult.Value).ConfigureAwait(false);
                if (!storeResult.IsSuccess)
                    return storeResult;

                return Result.Success();
            }

            return new FlowOperationPipeline<T>(WriteAsync, readPath: null);
        }

        /// <summary>
        /// Builds a full READ pipeline from the given environment:
        /// storage -> (verify) -> (decrypt) -> decompress -> deserialize -> T.
        /// </summary>
        public static FlowOperationPipeline<T> CreateReadPipeline(EnvironmentConfiguration env, string logicalKey)
        {
            if (env == null)
                throw new ArgumentNullException(nameof(env));

            if (string.IsNullOrWhiteSpace(logicalKey))
                throw new ArgumentException("Logical key is required.", nameof(logicalKey));

            // 1. Serializer (Result<byte[]> -> Result<T>)
            var serializerFactory = new SerializerFactory(env.SerializationOptions);
            var serializer = serializerFactory.CreateSerializer(env.SerializationOptions.SerializationType);

            Result<T> Deserialize(byte[] bytes) => serializer.Deserialize<T>(bytes);

            // 2. Storage provider (may obfuscate key internally)
            var storageFactory = new StorageProviderFactory(env.StorageOptions);
            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            // 3. Build byte[] -> Result<byte[]> pipeline (verify, decrypt, decompress) – reverse order of write
            var bytePipeline = BuildReadBytesPipeline(env);

            // 4. Final read delegate
            async Task<Result<T>> ReadAsync()
            {
                // read raw stored bytes
                var loadResult = await storage.LoadAsync(logicalKey).ConfigureAwait(false);
                if (!loadResult.IsSuccess)
                    return Result<T>.Failure(loadResult.Error);

                var bytes = loadResult.Value;

                // bytes -> processed bytes
                var pipeResult = await bytePipeline(bytes).ConfigureAwait(false);
                if (!pipeResult.IsSuccess)
                    return Result<T>.Failure(pipeResult.Error);

                // bytes -> T
                var deserResult = Deserialize(pipeResult.Value);
                if (!deserResult.IsSuccess)
                    return Result<T>.Failure(deserResult.Error);

                return Result<T>.Success(deserResult.Value);
            }

            return new FlowOperationPipeline<T>(writePath: null, readPath: ReadAsync);
        }

        // ------------------------------------------------------------
        //  INTERNAL HELPERS: CHAINING
        // ------------------------------------------------------------

        private static ByteStep Chain(ByteStep current, ByteStep next)
        {
            if (current == null) return next;

            return async bytes =>
            {
                var res = await current(bytes).ConfigureAwait(false);
                if (!res.IsSuccess)
                    return res;

                return await next(res.Value).ConfigureAwait(false);
            };
        }

        private static ByteStep IdentityStep =>
            bytes => Task.FromResult(Result<byte[]>.Success(bytes));

        // ------------------------------------------------------------
        //  WRITE path (byte[] -> Result<byte[]>)
        // ------------------------------------------------------------

        private static ByteStep BuildWriteBytesPipeline(EnvironmentConfiguration env)
        {
            ByteStep pipeline = IdentityStep;

            // 1) Compression
            bool compressionEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Compression) &&
                env.CompressionOptions != null &&
                env.CompressionOptions.CompressionType != CompressionType.None;

            ICompressor compressor;
            if (compressionEnabled)
            {
                var compressorFactory = new CompressorFactory();
                compressor = compressorFactory.CreateCompressor(env.CompressionOptions.CompressionType);
            }
            else
            {
                compressor = new NoOpCompressor();
            }

            pipeline = Chain(
                pipeline,
                bytes => Task.FromResult(compressor.Compress(bytes)));

            // 2) Encryption
            bool encryptionEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Encrypt) &&
                env.EncryptionOptions != null &&
                env.EncryptionOptions.EncryptionType != EncryptionType.None;

            IEncryptor encryptor;
            if (encryptionEnabled)
            {
                var encryptorFactory = new EncryptorFactory(env.EncryptionOptions, env.KeyStore);
                encryptor = encryptorFactory.CreateEncryptor(env.EncryptionOptions.EncryptionType);
            }
            else
            {
                encryptor = new NoOpEncryptor();
            }

            pipeline = Chain(
                pipeline,
                bytes => Task.FromResult(encryptor.Encrypt(bytes)));

            // 3) Signing
            bool signingEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Sign) &&
                env.SigningOptions != null &&
                env.SigningOptions.SigningType != SigningType.None;

            ISigner signer;
            if (signingEnabled)
            {
                var signerFactory = new SignerFactory(env.SigningOptions, env.KeyStore);
                signer = signerFactory.CreateSigner(env.SigningOptions.SigningType);
            }
            else
            {
                signer = new NoOpSigner();
            }

            pipeline = Chain(
                pipeline,
                bytes => Task.FromResult(signer.Sign(bytes)));

            return pipeline;
        }

        // ------------------------------------------------------------
        //  READ path (byte[] -> Result<byte[]>)
        // ------------------------------------------------------------

        private static ByteStep BuildReadBytesPipeline(EnvironmentConfiguration env)
        {
            ByteStep pipeline = IdentityStep;

            // IMPORTANT: reverse order compared to write:
            //  1) Verify signature
            //  2) Decrypt
            //  3) Decompress

            bool signingEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Sign) &&
                env.SigningOptions != null &&
                env.SigningOptions.SigningType != SigningType.None;

            ISigner signer;
            if (signingEnabled)
            {
                var signerFactory = new SignerFactory(env.SigningOptions, env.KeyStore);
                signer = signerFactory.CreateSigner(env.SigningOptions.SigningType);
            }
            else
            {
                signer = new NoOpSigner();
            }

            pipeline = Chain(
                pipeline,
                bytes => Task.FromResult(signer.Verify(bytes)));

            bool encryptionEnabled =
                env.Operations != null &&
                env.Operations.Contains(OperationMode.Encrypt) &&
                env.EncryptionOptions != null &&
                env.EncryptionOptions.EncryptionType != EncryptionType.None;

            IEncryptor encryptor;
            if (encryptionEnabled)
            {
                var encryptorFactory = new EncryptorFactory(env.EncryptionOptions, env.KeyStore);
                encryptor = encryptorFactory.CreateEncryptor(env.EncryptionOptions.EncryptionType);
            }
            else
            {
                encryptor = new NoOpEncryptor();
            }

            pipeline = Chain(
                pipeline,
                bytes => Task.FromResult(encryptor.Decrypt(bytes)));

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

            pipeline = Chain(
                pipeline,
                bytes => Task.FromResult(compressor.Decompress(bytes)));

            return pipeline;
        }
    }
}
