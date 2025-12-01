using Flowsave.Codec;
using FlowSave.Codec;
using FlowSave.Compression;
using FlowSave.Configurations;
using FlowSave.Encryption;
using FlowSave.KeyStorage;
using FlowSave.Serialization;
using FlowSave.Signing;
using FlowSave.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CompressionType = FlowSave.Compression.CompressionType;

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
        private delegate Task<Result<Envelope>> EnvelopeStep(Envelope input);

        // ------------------------------------------------------------
        //  FACTORIES
        // ------------------------------------------------------------

        /// <summary>
        /// Builds a full WRITE pipeline from the given environment:
        /// T -> serialize -> compress -> (encrypt) -> (sign) -> storage.
        /// </summary>
        public static FlowOperationPipeline<T> CreateWritePipeline(EnvironmentConfiguration env, KeyStoreOptions keyStore, string logicalKey)
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

            // 3. Envelope codec
            var envelopeCodec = CreateEnvelopeCodec(env);

            // 4. Build byte[] pipeline for payload (compress/encrypt/sign) – but now it will
            //    operate on Envelope.Payload and we'll also fill Operations/Signature.
            var payloadPipeline = BuildWritePayloadPipeline(env, keyStore);

            // 4. Final write delegate
            async Task<Result> WriteAsync(T value)
            {
                // T -> bytes (plain payload)
                var serResult = Serialize(value);
                if (!serResult.IsSuccess)
                    return Result.Failure(serResult.Error);

                var envelope = CreateBaseEnvelope(env, logicalKey, serResult.Value);

                // Apply operations on Envelope.Payload and record metadata
                var opsResult = await payloadPipeline(envelope).ConfigureAwait(false);
                if (!opsResult.IsSuccess)
                    return Result.Failure(opsResult.Error);

                envelope = opsResult.Value;

                // Encode envelope
                var encResult = envelopeCodec.Encode(envelope);
                if (!encResult.IsSuccess)
                    return Result.Failure(encResult.Error);

                // Store
                var storeResult = await storage.SaveAsync(logicalKey, encResult.Value).ConfigureAwait(false);
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
        public static FlowOperationPipeline<T> CreateReadPipeline(
            EnvironmentConfiguration env, KeyStoreOptions keyStore, string logicalKey)
        {
            if (env == null)
                throw new ArgumentNullException(nameof(env));
            if (string.IsNullOrWhiteSpace(logicalKey))
                throw new ArgumentException("Logical key is required.", nameof(logicalKey));

            // 1. Serializer
            var serializerFactory = new SerializerFactory(env.SerializationOptions);
            var serializer = serializerFactory.CreateSerializer(env.SerializationOptions.SerializationType);
            Result<T> Deserialize(byte[] bytes) => serializer.Deserialize<T>(bytes);

            // 2. Storage
            var storageFactory = new StorageProviderFactory(env.StorageOptions);
            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            // 3. Envelope codec
            var envelopeCodec = CreateEnvelopeCodec(env);

            // 4. Payload pipeline (verify/decrypt/decompress)
            var payloadPipeline = BuildReadPayloadPipeline(env, keyStore);

            async Task<Result<T>> ReadAsync()
            {
                // storage -> envelope bytes
                var loadResult = await storage.LoadAsync(logicalKey).ConfigureAwait(false);
                if (!loadResult.IsSuccess)
                    return Result<T>.Failure(loadResult.Error);

                // decode envelope
                var decResult = envelopeCodec.Decode(loadResult.Value);
                if (!decResult.IsSuccess)
                    return Result<T>.Failure(decResult.Error);

                var envelope = decResult.Value;

                // process payload bytes
                var opsResult = await payloadPipeline(envelope).ConfigureAwait(false);
                if (!opsResult.IsSuccess)
                    return Result<T>.Failure(opsResult.Error);

                envelope = opsResult.Value;

                // deserialize
                var deserResult = Deserialize(envelope.Payload);
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

        private static ByteStep BuildWriteBytesPipeline(EnvironmentConfiguration env, KeyStoreOptions keyStore)
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
                var encryptorFactory = new EncryptorFactory(env.EncryptionOptions, keyStore);
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
                var signerFactory = new SignerFactory(env.SigningOptions, keyStore);
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

        private static EnvelopeStep BuildWritePayloadPipeline(EnvironmentConfiguration env, KeyStoreOptions keyStore)
        {
            // We'll capture env + keystore & use the same logic as before,
            // but now we also populate OperationDescriptor entries.
            return async envelope =>
            {
                var ops = envelope.Operations ?? new List<OperationDescriptor>();
                var bytes = envelope.Payload;

                // 1) Compression
                bool compressionEnabled =
                    env.Operations != null &&
                    env.Operations.Contains(OperationMode.Compression) &&
                    env.CompressionOptions != null &&
                    env.CompressionOptions.CompressionType != CompressionType.None;

                if (compressionEnabled)
                {
                    var compressorFactory = new CompressorFactory();
                    var compressor = compressorFactory.CreateCompressor(env.CompressionOptions.CompressionType);

                    var compResult = compressor.Compress(bytes);
                    if (!compResult.IsSuccess)
                        return Result<Envelope>.Failure(compResult.Error);

                    bytes = compResult.Value;

                    ops.Add(new OperationDescriptor
                    {
                        Kind = "compress",
                        AlgorithmId = env.CompressionOptions.CompressionType.ToString(),
                        KeyId = null,
                        Parameters = null
                    });
                }

                // 2) Encryption
                bool encryptionEnabled =
                    env.Operations != null &&
                    env.Operations.Contains(OperationMode.Encrypt) &&
                    env.EncryptionOptions != null &&
                    env.EncryptionOptions.EncryptionType != EncryptionType.None;

                if (encryptionEnabled)
                {
                    var encryptorFactory = new EncryptorFactory(env.EncryptionOptions, keyStore);
                    var encryptor = encryptorFactory.CreateEncryptor(env.EncryptionOptions.EncryptionType);

                    var encResult = encryptor.Encrypt(bytes);
                    if (!encResult.IsSuccess)
                        return Result<Envelope>.Failure(encResult.Error);

                    bytes = encResult.Value;

                    ops.Add(new OperationDescriptor
                    {
                        Kind = "encrypt",
                        AlgorithmId = env.EncryptionOptions.EncryptionType.ToString(),
                        KeyId = env.EncryptionOptions.KeyId, // or wherever you store it
                        Parameters = null // IV/nonce could go here if you expose it
                    });
                }

                // 3) Signing
                bool signingEnabled =
                    env.Operations != null &&
                    env.Operations.Contains(OperationMode.Sign) &&
                    env.SigningOptions != null &&
                    env.SigningOptions.SigningType != SigningType.None;

                SignatureBlock signature = envelope.Signature;

                if (signingEnabled)
                {
                    var signerFactory = new SignerFactory(env.SigningOptions, keyStore);
                    var signer = signerFactory.CreateSigner(env.SigningOptions.SigningType);

                    var signResult = signer.Sign(bytes);
                    if (!signResult.IsSuccess)
                        return Result<Envelope>.Failure(signResult.Error);

                    // NOTE: for now we assume the signer returns the *same bytes* (signature not embedded),
                    // and you separately expose SignatureBlock via env.SigningOptions or signer.
                    // If your current signer appends signature into bytes, you can:
                    //   - leave bytes = signResult.Value
                    //   - set Signature = null (envelope doesn't know)
                    // and refactor signers later to expose SignatureBlock.
                    bytes = signResult.Value;

                    ops.Add(new OperationDescriptor
                    {
                        Kind = "sign",
                        AlgorithmId = env.SigningOptions.SigningType.ToString(),
                        KeyId = env.SigningOptions.KeyId,
                        Parameters = null
                    });

                    // TODO: when you change ISigner to expose signature separately:
                    // signature = new SignatureBlock { AlgorithmId = ..., KeyId = ..., Value = macBytes };
                }

                envelope.Payload = bytes;
                envelope.Operations = ops;
                envelope.Signature = signature; // currently null until signer is refactored

                return Result<Envelope>.Success(envelope);
            };
        }


        // ------------------------------------------------------------
        //  READ path (byte[] -> Result<byte[]>)
        // ------------------------------------------------------------

        private static ByteStep BuildReadBytesPipeline(EnvironmentConfiguration env, KeyStoreOptions keyStore)
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
                var signerFactory = new SignerFactory(env.SigningOptions, keyStore);
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
                var encryptorFactory = new EncryptorFactory(env.EncryptionOptions, keyStore);
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

        private static EnvelopeStep BuildReadPayloadPipeline(EnvironmentConfiguration env, KeyStoreOptions keyStore)
        {
            return async envelope =>
            {
                var bytes = envelope.Payload;

                // 1) Verify (reverse of sign)
                bool signingEnabled =
                    env.Operations != null &&
                    env.Operations.Contains(OperationMode.Sign) &&
                    env.SigningOptions != null &&
                    env.SigningOptions.SigningType != SigningType.None;

                if (signingEnabled)
                {
                    var signerFactory = new SignerFactory(env.SigningOptions, keyStore);
                    var signer = signerFactory.CreateSigner(env.SigningOptions.SigningType);

                    var verResult = signer.Verify(bytes);
                    if (!verResult.IsSuccess)
                        return Result<Envelope>.Failure(verResult.Error);

                    bytes = verResult.Value;
                }

                // 2) Decrypt
                bool encryptionEnabled =
                    env.Operations != null &&
                    env.Operations.Contains(OperationMode.Encrypt) &&
                    env.EncryptionOptions != null &&
                    env.EncryptionOptions.EncryptionType != EncryptionType.None;

                if (encryptionEnabled)
                {
                    var encryptorFactory = new EncryptorFactory(env.EncryptionOptions, keyStore);
                    var encryptor = encryptorFactory.CreateEncryptor(env.EncryptionOptions.EncryptionType);

                    var decResult = encryptor.Decrypt(bytes);
                    if (!decResult.IsSuccess)
                        return Result<Envelope>.Failure(decResult.Error);

                    bytes = decResult.Value;
                }

                // 3) Decompress
                bool compressionEnabled =
                    env.Operations != null &&
                    env.Operations.Contains(OperationMode.Compression) &&
                    env.CompressionOptions != null &&
                    env.CompressionOptions.CompressionType != CompressionType.None;

                if (compressionEnabled)
                {
                    var factory = new CompressorFactory();
                    var compressor = factory.CreateCompressor(env.CompressionOptions.CompressionType);

                    var decompResult = compressor.Decompress(bytes);
                    if (!decompResult.IsSuccess)
                        return Result<Envelope>.Failure(decompResult.Error);

                    bytes = decompResult.Value;
                }

                envelope.Payload = bytes;
                return Result<Envelope>.Success(envelope);
            };
        }


        private static IEnvelopeCodec CreateEnvelopeCodec(EnvironmentConfiguration env)
        {
            // Minimal first version: always binary.
            // Later you can branch to JsonEnvelopeCodec in editor/dev via env or defines.
            return new BinaryEnvelopeCodec();
        }

        private static Envelope CreateBaseEnvelope(EnvironmentConfiguration env, string namespaceId, byte[] payload)
        {
            return new Envelope
            {
                FileSignature = EnvelopeConstants.FileSignature,
                EnvelopeVersion = EnvelopeConstants.CurrentEnvelopeVersion,
                NamespaceId = namespaceId,
                //DataVersion = env.SerializationOptions.DataVersion, // or wherever you keep it
                PayloadFormat = env.SerializationOptions.SerializationType, // maps to your SerializationType

                Creator = new CreatorInfo
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    AppVersion = Application.version,  // if you have one
                    //BuildId = env.BuildId,            // optional
                    //DeviceId = null                    // optional
                },

                Operations = new List<OperationDescriptor>(),
                Signature = null,
                Payload = payload
            };
        }

    }
}
