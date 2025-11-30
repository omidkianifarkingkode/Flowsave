using FlowSave.KeyStorage;
using System;

namespace FlowSave.Encryption
{
    public class EncryptorFactory : IEncryptorFactory
    {
        private readonly EncryptionOptions _options;
        private readonly KeyStoreOptions _keys;

        public EncryptorFactory(EncryptionOptions options, KeyStoreOptions keys)
        {
            _options = options;
            _keys = keys;
        }

        public IEncryptor CreateEncryptor(EncryptionType encryptionType)
        {
            return encryptionType switch
            {
                EncryptionType.None => new NoOpEncryptor(),
                EncryptionType.Aes128Cbc => CreateAesEncryptor(_options.Aes128KeyId),
                EncryptionType.Aes256Cbc => CreateAesEncryptor(_options.Aes256KeyId),
                _ => throw new NotSupportedException(
                    $"The specified crypto algorithm '{encryptionType}' is not supported."),
            };
        }

        private IEncryptor CreateAesEncryptor(string keyId)
        {
            var def = _keys.GetAesDefinition(keyId);
            if (def == null)
                throw new InvalidOperationException(
                    $"No AES key definition found for id '{keyId}' in KeyStore.");

            return new AesCbcEncryptor(def);
        }
    }
}
