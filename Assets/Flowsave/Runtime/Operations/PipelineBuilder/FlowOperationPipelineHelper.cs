using FlowSave.Codec;
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
using System.Linq;
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

            if (env.StorageOptions.StorageType == StorageType.FileSystem && env.StorageOptions.DiskStorage.Append == true)
                return CreateAppendWritePipeline(env, keyStore, logicalKey);

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
        public static FlowOperationPipeline<T> CreateReadPipeline(EnvironmentConfiguration env, KeyStoreOptions keyStore, string logicalKey)
        {
            if (env == null)
                throw new ArgumentNullException(nameof(env));
            if (string.IsNullOrWhiteSpace(logicalKey))
                throw new ArgumentException("Logical key is required.", nameof(logicalKey));

            if (env.StorageOptions.StorageType == StorageType.FileSystem && env.StorageOptions.DiskStorage.Append == true)
                return CreateAppendReadPipeline(env, keyStore, logicalKey);

            // 1. Storage
            var storageFactory = new StorageProviderFactory(env.StorageOptions);
            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            // 2. Envelope codec
            var envelopeCodec = CreateEnvelopeCodec(env);

            // 3. Payload pipeline (verify/decrypt/decompress) – now envelope-driven
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

                // process payload bytes according to envelope.Operations
                var opsResult = await payloadPipeline(envelope).ConfigureAwait(false);
                if (!opsResult.IsSuccess)
                    return Result<T>.Failure(opsResult.Error);

                envelope = opsResult.Value;

                // 4. Deserialize using envelope.PayloadFormat, not env.SerializationOptions
                var serializerFactory = new SerializerFactory(env.SerializationOptions);
                var serializer = serializerFactory.CreateSerializer(envelope.PayloadFormat);

                var deserResult = serializer.Deserialize<T>(envelope.Payload);
                if (!deserResult.IsSuccess)
                    return Result<T>.Failure(deserResult.Error);

                return Result<T>.Success(deserResult.Value);
            }

            return new FlowOperationPipeline<T>(writePath: null, readPath: ReadAsync);
        }

        public static Func<Task<Result<T[]>>> CreateAppendReadAll(EnvironmentConfiguration env, KeyStoreOptions keyStore, string logicalKey)
        {
            return async () =>
            {
                var storageFactory = new StorageProviderFactory(env.StorageOptions);
                var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

                var load = await storage.LoadAsync(logicalKey).ConfigureAwait(false);
                if (!load.IsSuccess)
                    return Result<T[]>.Failure(load.Error);

                var buffer = load.Value;
                int offset = 0;

                // 1) Header
                if (buffer.Length < 4)
                    return Result<T[]>.Failure("Append log too small (missing header length).");

                int headerLen = BitConverter.ToInt32(buffer, offset);
                offset += 4;

                if (buffer.Length < offset + headerLen)
                    return Result<T[]>.Failure("Append log truncated (header).");

                var headerBytes = new byte[headerLen];
                Buffer.BlockCopy(buffer, offset, headerBytes, 0, headerLen);
                offset += headerLen;

                var codec = CreateEnvelopeCodec(env);
                var headerResult = codec.Decode(headerBytes);
                if (!headerResult.IsSuccess)
                    return Result<T[]>.Failure(headerResult.Error);

                var headerEnv = headerResult.Value;
                // Optionally: validate headerEnv.Operations vs env.Operations

                // 2) Build byte pipeline for READ (legacy-style)
                var bytePipe = BuildReadBytesPipeline(env, keyStore);

                // 3) Serializer (use headerEnv.PayloadFormat, same as top-level)
                var serializerFactory = new SerializerFactory(env.SerializationOptions);
                var serializer = serializerFactory.CreateSerializer(headerEnv.PayloadFormat);

                var list = new List<T>();

                while (offset + 4 <= buffer.Length)
                {
                    int recLen = BitConverter.ToInt32(buffer, offset);
                    offset += 4;

                    if (recLen <= 0 || offset + recLen > buffer.Length)
                        return Result<T[]>.Failure("Append log truncated (record).");

                    var recBytes = new byte[recLen];
                    Buffer.BlockCopy(buffer, offset, recBytes, 0, recLen);
                    offset += recLen;

                    // signedEnvelope -> verify -> decrypt -> decompress
                    var pipe = await bytePipe(recBytes).ConfigureAwait(false);
                    if (!pipe.IsSuccess)
                        return Result<T[]>.Failure(pipe.Error);

                    var des = serializer.Deserialize<T>(pipe.Value);
                    if (!des.IsSuccess)
                        return Result<T[]>.Failure(des.Error);

                    list.Add(des.Value);
                }

                return Result<T[]>.Success(list.ToArray());
            };
        }

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
                        Kind = OperationMode.Compression,
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
                        Kind = OperationMode.Encrypt,
                        AlgorithmId = env.EncryptionOptions.EncryptionType.ToString(),
                        KeyId = encryptor.KeyId,
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

                    var sigResult = signer.ComputeSignature(bytes);
                    if (!sigResult.IsSuccess)
                        return Result<Envelope>.Failure(sigResult.Error);

                    signature = new SignatureBlock
                    {
                        AlgorithmId = env.SigningOptions.SigningType.ToString(),
                        KeyId = signer.KeyId,
                        Value = sigResult.Value
                    };
                }

                // compression/encryption already updated bytes and ops
                envelope.Payload = bytes;
                envelope.Operations = ops;       // only Compression / Encrypt
                envelope.Signature = signature; // signature-only metadata

                return Result<Envelope>.Success(envelope);

            };
        }

        private static FlowOperationPipeline<T> CreateAppendWritePipeline(EnvironmentConfiguration env, KeyStoreOptions keyStore, string logicalKey)
        {
            // 1. Serializer
            var serializerFactory = new SerializerFactory(env.SerializationOptions);
            var serializer = serializerFactory.CreateSerializer(env.SerializationOptions.SerializationType);

            // 2. Storage
            var storageFactory = new StorageProviderFactory(env.StorageOptions);
            var storage = storageFactory.CreateStorageProvider(env.StorageOptions.StorageType);

            // 3. Per-record byte pipeline: compress -> encrypt -> sign (envelope-style)
            var bytePipeline = BuildWriteBytesPipeline(env, keyStore);

            async Task<Result> AppendAsync(T value)
            {
                // T -> bytes
                var ser = serializer.Serialize(value);
                if (!ser.IsSuccess)
                    return Result.Failure(ser.Error);

                // bytes -> signed envelope (HMAC wrapper etc.)
                var pipe = await bytePipeline(ser.Value).ConfigureAwait(false);
                if (!pipe.IsSuccess)
                    return Result.Failure(pipe.Error);

                var recordBytes = pipe.Value;

                // Check existence (we need header only once)
                var exists = await storage.ExistsAsync(logicalKey).ConfigureAwait(false);
                if (!exists.IsSuccess)
                    return Result.Failure(exists.Error);

                if (!exists.Value)
                {
                    // First write: header + first record
                    var headerEnv = CreateBaseEnvelope(env, logicalKey, Array.Empty<byte>());
                    headerEnv.Signature = null; // header itself not signed

                    var codec = CreateEnvelopeCodec(env);
                    var headerResult = codec.Encode(headerEnv);
                    if (!headerResult.IsSuccess)
                        return Result.Failure(headerResult.Error);

                    var headerBytes = headerResult.Value;
                    var headerLenBytes = BitConverter.GetBytes(headerBytes.Length);
                    var recLenBytes = BitConverter.GetBytes(recordBytes.Length);

                    var combined = new byte[
                        headerLenBytes.Length + headerBytes.Length +
                        recLenBytes.Length + recordBytes.Length];

                    Buffer.BlockCopy(headerLenBytes, 0, combined, 0, headerLenBytes.Length);
                    Buffer.BlockCopy(headerBytes, 0, combined, headerLenBytes.Length, headerBytes.Length);
                    Buffer.BlockCopy(recLenBytes, 0, combined, headerLenBytes.Length + headerBytes.Length, recLenBytes.Length);
                    Buffer.BlockCopy(recordBytes, 0, combined, headerLenBytes.Length + headerBytes.Length + recLenBytes.Length, recordBytes.Length);

                    return await storage.SaveAsync(logicalKey, combined).ConfigureAwait(false);
                }
                else
                {
                    // Later writes: just append record
                    var recLenBytes = BitConverter.GetBytes(recordBytes.Length);
                    var frame = new byte[recLenBytes.Length + recordBytes.Length];
                    Buffer.BlockCopy(recLenBytes, 0, frame, 0, recLenBytes.Length);
                    Buffer.BlockCopy(recordBytes, 0, frame, recLenBytes.Length, recordBytes.Length);

                    // DiskStorageProvider will append because DiskStorageOptions.Append == true
                    return await storage.SaveAsync(logicalKey, frame).ConfigureAwait(false);
                }
            }

            return new FlowOperationPipeline<T>(AppendAsync, readPath: null);
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
                var ops = envelope.Operations;

                // 1) Verify signature if present
                if (envelope.Signature != null &&
                    envelope.Signature.Value != null &&
                    envelope.Signature.Value.Length > 0)
                {
                    if (env.SigningOptions == null)
                        return Result<Envelope>.Failure("Envelope has signature but env.SigningOptions is null.");

                    var sig = envelope.Signature;

                    var signType = env.SigningOptions.SigningType;
                    if (!string.IsNullOrEmpty(sig.AlgorithmId) &&
                        Enum.TryParse(sig.AlgorithmId, ignoreCase: true, out SigningType parsedSignType))
                    {
                        signType = parsedSignType;
                    }

                    var signerFactory = new SignerFactory(env.SigningOptions, keyStore);

                    ISigner signer =
                        !string.IsNullOrEmpty(sig.KeyId)
                            ? signerFactory.CreateSigner(signType, sig.KeyId)
                            : signerFactory.CreateSigner(signType);

                    var verifyResult = signer.VerifySignature(bytes, sig.Value);
                    if (!verifyResult.IsSuccess)
                        return Result<Envelope>.Failure(verifyResult.Error);
                }

                // 2) Decrypt (if there's an Encrypt op)
                var encOp = ops?.FirstOrDefault(o => o.Kind == OperationMode.Encrypt);
                if (encOp != null)
                {
                    if (env.EncryptionOptions == null)
                        return Result<Envelope>.Failure("Envelope contains encrypt operation but env.EncryptionOptions is null.");

                    var encType = env.EncryptionOptions.EncryptionType;
                    if (!string.IsNullOrEmpty(encOp.AlgorithmId) &&
                        Enum.TryParse(encOp.AlgorithmId, ignoreCase: true, out EncryptionType parsedEncType))
                    {
                        encType = parsedEncType;
                    }

                    var encryptorFactory = new EncryptorFactory(env.EncryptionOptions, keyStore);

                    IEncryptor encryptor =
                        !string.IsNullOrEmpty(encOp.KeyId)
                            ? encryptorFactory.CreateEncryptor(encType, encOp.KeyId)
                            : encryptorFactory.CreateEncryptor(encType);

                    var decResult = encryptor.Decrypt(bytes);
                    if (!decResult.IsSuccess)
                        return Result<Envelope>.Failure(decResult.Error);

                    bytes = decResult.Value;
                }

                // 3) Decompress (if there's a Compression op)
                var compOp = ops?.FirstOrDefault(o => o.Kind == OperationMode.Compression);
                if (compOp != null)
                {
                    if (env.CompressionOptions == null)
                        return Result<Envelope>.Failure("Envelope contains compress operation but env.CompressionOptions is null.");

                    var compType = env.CompressionOptions.CompressionType;
                    if (!string.IsNullOrEmpty(compOp.AlgorithmId) &&
                        Enum.TryParse(compOp.AlgorithmId, ignoreCase: true, out CompressionType parsedCompType))
                    {
                        compType = parsedCompType;
                    }

                    var factory = new CompressorFactory();
                    var compressor = factory.CreateCompressor(compType);

                    var decompResult = compressor.Decompress(bytes);
                    if (!decompResult.IsSuccess)
                        return Result<Envelope>.Failure(decompResult.Error);

                    bytes = decompResult.Value;
                }

                envelope.Payload = bytes;
                return Result<Envelope>.Success(envelope);
            };
        }

        private static FlowOperationPipeline<T> CreateAppendReadPipeline(EnvironmentConfiguration env, KeyStoreOptions keyStore, string logicalKey)
        {
            var readAll = CreateAppendReadAll(env, keyStore, logicalKey);

            // For "normal" LoadAsync<T> on append mode, we can decide to return last entry:
            async Task<Result<T>> ReadLastAsync()
            {
                var all = await readAll().ConfigureAwait(false);
                if (!all.IsSuccess)
                    return Result<T>.Failure(all.Error);

                if (all.Value.Length == 0)
                    return Result<T>.Failure("No entries in append log.");

                return Result<T>.Success(all.Value[all.Value.Length - 1]);
            }

            return new FlowOperationPipeline<T>(
                writePath: null,
                readPath: ReadLastAsync);
        }


        // ------------------------------------------------------------
        //  INTERNAL HELPERS
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
