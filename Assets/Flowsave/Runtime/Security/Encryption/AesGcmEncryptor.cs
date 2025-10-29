using System;
using System.Security.Cryptography;

namespace Flowsave.Security
{
    public sealed class AesGcmEncryptor : IEncryptor
    {
        private readonly byte[] _key;
        public CryptoAlgId Alg => _key.Length == 32 ? CryptoAlgId.Aes256Gcm : CryptoAlgId.Aes128Gcm;
        public int NonceSize => 12;
        public int TagSize => 16;
        public NonceStrategy Strategy { get; }


        public AesGcmEncryptor(byte[] key, NonceStrategy strategy = NonceStrategy.Random)
        {
            if (key is null || (key.Length != 16 && key.Length != 32))
                throw new ArgumentException("AES key must be 16 or 32 bytes", nameof(key));
            _key = (byte[])key.Clone();
            Strategy = strategy;
        }


        public (byte[] Nonce, byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad)
        {
            // For GCM, Random nonces are recommended in most app scenarios
            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            byte[] cipher = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];

            using var gcm = new AesGcm(_key);
            gcm.Encrypt(nonce, plaintext, cipher, tag, aad);

            return (nonce, cipher, tag);
        }


        public byte[] Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad)
        {
            if (nonce.Length != NonceSize) throw new ArgumentException($"Nonce must be {NonceSize} bytes for AES-GCM.");
            if (tag.Length != TagSize) throw new ArgumentException($"Tag must be {TagSize} bytes for AES-GCM.");

            byte[] plain = new byte[ciphertext.Length];
            using var gcm = new AesGcm(_key);
            gcm.Decrypt(nonce, ciphertext, tag, plain, aad); // throws on auth failure

            return plain;
        }
    }
}
