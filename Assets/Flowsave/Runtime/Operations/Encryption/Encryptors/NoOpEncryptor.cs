using System;

namespace Flowsave.Operations
{
    public sealed class NoOpEncryptor : IEncryptor
    {
        public EncryptionType Alg => EncryptionType.None;
        public bool IsNoOp => true;

        public Result<byte[]> Encrypt(byte[] plaintext)
        {
            if (plaintext == null)
                return Result<byte[]>.Failure("Plaintext is null.");

            // No encryption → return the same reference
            return Result<byte[]>.Success(plaintext);
        }

        public Result<byte[]> Decrypt(byte[] encryptedEnvelope)
        {
            if (encryptedEnvelope == null)
                return Result<byte[]>.Failure("Encrypted payload is null.");

            // No decryption → return the same reference
            return Result<byte[]>.Success(encryptedEnvelope);
        }
    }
}
