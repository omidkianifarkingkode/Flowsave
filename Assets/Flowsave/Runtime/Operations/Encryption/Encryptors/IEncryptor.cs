using System;

namespace FlowSave.Encryption
{
    public interface IEncryptor
    {
        EncryptionType Alg { get; }
        bool IsNoOp { get; }


        /// <summary>Encrypts plaintext and returns an envelope (nonce+tag+ciphertext, or whatever format).</summary>
        Result<byte[]> Encrypt(byte[] plaintext);

        /// <summary>Decrypts a previously produced envelope.</summary>
        Result<byte[]> Decrypt(byte[] encryptedEnvelope);
    }
}
