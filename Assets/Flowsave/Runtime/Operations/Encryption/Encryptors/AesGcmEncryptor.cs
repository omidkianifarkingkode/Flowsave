using System;
using System.Security.Cryptography;
using Flowsave; // for Result<T>

namespace Flowsave.Operations
{
    public sealed class AesGcmEncryptor : IEncryptor
    {
        private readonly byte[] _key;

        // We still expose the algorithm identity
        public EncryptionType Alg => _key.Length == 32
            ? EncryptionType.Aes256Gcm
            : EncryptionType.Aes128Gcm;

        public bool IsNoOp => false;

        // Internal constants – GCM standard sizes
        private const int NonceSize = 12; // 96-bit nonce
        private const int TagSize = 16;   // 128-bit tag

        public AesGcmEncryptor(byte[] key)
        {
            if (key is null || (key.Length != 16 && key.Length != 32))
                throw new ArgumentException("AES key must be 16 or 32 bytes", nameof(key));

            _key = (byte[])key.Clone();
        }

        public AesGcmEncryptor(AesOptions options) : this(options.Key)
        {
            // You can optionally use options.Nonce / TagBytes here
            // for strategy/customization if you want in the future.
        }

        /// <summary>
        /// Encrypts plaintext and returns an envelope:
        /// [algId:1][nonce:12][tag:16][ciphertext...]
        /// </summary>
        public Result<byte[]> Encrypt(byte[] plaintext)
        {
            if (plaintext == null)
                return Result<byte[]>.Failure("Plaintext is null.");

            try
            {
                byte[] nonce = new byte[NonceSize];
                RandomNumberGenerator.Fill(nonce);

                byte[] cipher = new byte[plaintext.Length];
                byte[] tag = new byte[TagSize];

                using (var gcm = new AesGcm(_key))
                {
                    // AAD is null for now – can be extended later
                    gcm.Encrypt(nonce, plaintext, cipher, tag, associatedData: null);
                }

                // Envelope layout: [algId][nonce][tag][cipher]
                var result = new byte[1 + nonce.Length + tag.Length + cipher.Length];

                int offset = 0;
                result[offset++] = (byte)Alg;

                Buffer.BlockCopy(nonce, 0, result, offset, nonce.Length);
                offset += nonce.Length;

                Buffer.BlockCopy(tag, 0, result, offset, tag.Length);
                offset += tag.Length;

                Buffer.BlockCopy(cipher, 0, result, offset, cipher.Length);

                return Result<byte[]>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Decrypts an envelope produced by Encrypt:
        /// [algId][nonce][tag][ciphertext...] -> plaintext
        /// </summary>
        public Result<byte[]> Decrypt(byte[] encryptedEnvelope)
        {
            if (encryptedEnvelope == null)
                return Result<byte[]>.Failure("Encrypted data is null.");

            try
            {
                if (encryptedEnvelope.Length < 1 + NonceSize + TagSize)
                    return Result<byte[]>.Failure("Encrypted payload is too short.");

                int offset = 0;

                var alg = (EncryptionType)encryptedEnvelope[offset++];
                if (alg != Alg)
                    return Result<byte[]>.Failure($"Unexpected encryption algorithm: {alg} (expected {Alg}).");

                var nonce = new byte[NonceSize];
                Buffer.BlockCopy(encryptedEnvelope, offset, nonce, 0, nonce.Length);
                offset += nonce.Length;

                var tag = new byte[TagSize];
                Buffer.BlockCopy(encryptedEnvelope, offset, tag, 0, tag.Length);
                offset += tag.Length;

                var ciphertext = new byte[encryptedEnvelope.Length - offset];
                Buffer.BlockCopy(encryptedEnvelope, offset, ciphertext, 0, ciphertext.Length);

                var plain = new byte[ciphertext.Length];

                using (var gcm = new AesGcm(_key))
                {
                    gcm.Decrypt(nonce, ciphertext, tag, plain, associatedData: null);
                }

                return Result<byte[]>.Success(plain);
            }
            catch (CryptographicException ex)
            {
                // Auth/tag failure, tampering, wrong key etc.
                return Result<byte[]>.Failure($"Decryption/authentication failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }
    }
}
