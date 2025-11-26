using System;

namespace Flowsave.Operations
{
    public class EncryptorFactory : IEncryptorFactory
    {
        private readonly EncryptionOptions _options;

        public EncryptorFactory(EncryptionOptions options)
        {
            _options = options;
        }

        public IEncryptor CreateEncryptor(EncryptionType encryptionType)
        {
            return encryptionType switch
            {
                EncryptionType.None => new NoOpEncryptor(),
                EncryptionType.Aes128Gcm => new AesGcmEncryptor(_options.Aes128),
                EncryptionType.Aes256Gcm => new AesGcmEncryptor(_options.Aes256),
                _ => throw new NotSupportedException($"The specified crypto algorithm '{encryptionType}' is not supported."),
            };
        }
    }
}
