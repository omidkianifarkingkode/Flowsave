using System;

namespace FlowSave.Encryption
{
    public sealed class NoOpEncryptor : IEncryptor
    {
        public EncryptionType Alg => EncryptionType.None;
        public bool IsNoOp => true;
        public string KeyId => string.Empty;

        public Result<byte[]> Encrypt(byte[] plaintext)
        {
            if (plaintext == null)
                return EncryptionErrors.PlaintextNull;

            // No encryption → return the same reference
            return Result<byte[]>.Success(plaintext);
        }

        public Result<byte[]> Decrypt(byte[] encryptedEnvelope)
        {
            if (encryptedEnvelope == null)
                return EncryptionErrors.EncryptedPayloadNull;

            // No decryption → return the same reference
            return Result<byte[]>.Success(encryptedEnvelope);
        }
    }
}
