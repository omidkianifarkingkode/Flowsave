using FlowSave.KeyStorage;
using System;
using System.Security.Cryptography;

namespace FlowSave.Encryption
{
    public sealed class AesCbcEncryptor : IEncryptor
    {
        private readonly byte[] _key;

        public EncryptionType Alg { get; }
        public bool IsNoOp => false;
        public string KeyId { get; }

        private const int IvSize = 16; // AES block size

        public AesCbcEncryptor(KeyDefinition def)
        {
            var key = KeyRuntime.ResolveAesKey(def);

            KeyId = def.KeyId;

            if (key is null || (key.Length != 16 && key.Length != 32))
                throw new ArgumentException("AES key must be 16 or 32 bytes", nameof(key));

            _key = (byte[])key.Clone();

            Alg = key.Length == 32 ? EncryptionType.Aes256Cbc : EncryptionType.Aes128Cbc;
        }

        // ============================================================

        /// <summary>
        /// Envelope: [algId:1][iv:16][ciphertext...]
        /// </summary>
        public Result<byte[]> Encrypt(byte[] plaintext)
        {
            if (plaintext == null)
                return EncryptionErrors.PlaintextNull;

            try
            {
                // Random IV per encryption
                byte[] iv = new byte[IvSize];
                RandomNumberGenerator.Fill(iv);

                byte[] cipher;

                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var enc = aes.CreateEncryptor();
                    cipher = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);
                }

                byte[] result = new byte[1 + iv.Length + cipher.Length];

                int offset = 0;
                result[offset++] = (byte)Alg;
                Buffer.BlockCopy(iv, 0, result, offset, iv.Length); offset += iv.Length;
                Buffer.BlockCopy(cipher, 0, result, offset, cipher.Length);

                return Result<byte[]>.Success(result);
            }
            catch (Exception ex)
            {
                return EncryptionErrors.EncryptFailed(ex.Message);
            }
        }

        // ============================================================

        public Result<byte[]> Decrypt(byte[] env)
        {
            if (env == null)
                return EncryptionErrors.EncryptedEnvelopeNull;

            try
            {
                if (env.Length < 1 + IvSize)
                    return EncryptionErrors.EnvelopeTooSmall;

                int offset = 0;

                EncryptionType alg = (EncryptionType)env[offset++];
                if (alg != Alg)
                    return EncryptionErrors.WrongAlgorithm(Alg, alg);

                byte[] iv = new byte[IvSize];
                Buffer.BlockCopy(env, offset, iv, 0, iv.Length);
                offset += iv.Length;

                byte[] cipher = new byte[env.Length - offset];
                if (cipher.Length == 0)
                    return EncryptionErrors.CiphertextEmpty;

                Buffer.BlockCopy(env, offset, cipher, 0, cipher.Length);

                byte[] plaintext;

                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var dec = aes.CreateDecryptor();
                    plaintext = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                }

                return Result<byte[]>.Success(plaintext);
            }
            catch (CryptographicException ex)
            {
                return EncryptionErrors.DecryptCryptoFailed(ex.Message);
            }
            catch (Exception ex)
            {
                return EncryptionErrors.DecryptFailed(ex.Message);
            }
        }
    }
}
