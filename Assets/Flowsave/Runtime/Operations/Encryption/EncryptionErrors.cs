namespace FlowSave.Encryption
{
    public static class EncryptionErrors
    {
        private const string EncryptionPrefix = "[FlowSave]:[Encryption]:";
        private const string AesCbcPrefix = "[FlowSave]:[AesCbcEncryptor]:";

        public static readonly Result<byte[]> PlaintextNull = Result<byte[]>.Failure($"{EncryptionPrefix}Plaintext is null.");
        public static readonly Result<byte[]> EncryptedPayloadNull = Result<byte[]>.Failure($"{EncryptionPrefix}Encrypted payload is null.");
        public static readonly Result<byte[]> EncryptedEnvelopeNull = Result<byte[]>.Failure($"{EncryptionPrefix}Encrypted envelope is null.");
        public static readonly Result<byte[]> EnvelopeTooSmall = Result<byte[]>.Failure($"{AesCbcPrefix}Envelope is too small.");
        public static readonly Result<byte[]> CiphertextEmpty = Result<byte[]>.Failure($"{AesCbcPrefix}Ciphertext is empty.");

        public static Result<byte[]> WrongAlgorithm(EncryptionType expected, EncryptionType actual)
            => Result<byte[]>.Failure($"{AesCbcPrefix}Wrong algorithm. Expected {expected}, got {actual}");

        public static Result<byte[]> EncryptFailed(string message)
            => Result<byte[]>.Failure($"{AesCbcPrefix}AES-CBC encrypt failed: {message}");

        public static Result<byte[]> DecryptFailed(string message)
            => Result<byte[]>.Failure($"{AesCbcPrefix}AES-CBC decrypt failed: {message}");

        public static Result<byte[]> DecryptCryptoFailed(string message)
            => Result<byte[]>.Failure($"{AesCbcPrefix}AES-CBC decryption failed (wrong key or corrupted data): {message}");
    }
}
