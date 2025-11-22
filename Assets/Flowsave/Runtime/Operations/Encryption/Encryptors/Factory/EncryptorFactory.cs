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

        public IEncryptor CreateSigner(EncryptionType cryptoAlgId)
        {
            return cryptoAlgId switch
            {
                EncryptionType.Aes128Gcm => new AesGcmEncryptor(_options.Aes128),
                EncryptionType.Aes256Gcm => new AesGcmEncryptor(_options.Aes256),
                _ => throw new NotSupportedException($"The specified crypto algorithm '{cryptoAlgId}' is not supported."),
            };
        }
    }
}
