namespace Flowcast.FlowSave
{
    /// <summary>
    /// Optional service that encrypts and decrypts payloads before persistence.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts the provided payload.
        /// </summary>
        /// <param name="data">Data to encrypt.</param>
        /// <returns>Encrypted payload.</returns>
        byte[] Encrypt(byte[] data);

        /// <summary>
        /// Decrypts the provided payload.
        /// </summary>
        /// <param name="data">Data to decrypt.</param>
        /// <returns>Decrypted payload.</returns>
        byte[] Decrypt(byte[] data);
    }
}
