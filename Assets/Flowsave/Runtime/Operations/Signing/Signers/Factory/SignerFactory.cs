using FlowSave.KeyStorage;
using System;

namespace FlowSave.Signing
{
    public class SignerFactory : ISignerFactory
    {
        private readonly SigningOptions _options;
        private readonly KeyStoreOptions _keys;

        public SignerFactory(SigningOptions options, KeyStoreOptions keys)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        public ISigner CreateSigner(SigningType signAlg)
        {
            return signAlg switch
            {
                SigningType.None => new NoOpSigner(),
                SigningType.Hmac => CreateHmacSigner(_options.HmacKeyId),
                _ => throw new InvalidOperationException(
                    $"Unsupported signing algorithm: {signAlg}")
            };
        }

        public ISigner CreateSigner(SigningType signAlg, string keyId)
        {
            return signAlg switch
            {
                SigningType.None => new NoOpSigner(),
                SigningType.Hmac => CreateHmacSigner(keyId),
                _ => throw new InvalidOperationException(
                    $"Unsupported signing algorithm: {signAlg}")
            };
        }

        private ISigner CreateHmacSigner(string keyId)
        {
            var def = _keys.GetHmacDefinition(keyId);
            if (def == null)
                throw new InvalidOperationException(
                    $"No HMAC key definition found for id '{keyId}' in KeyStore.");

            return new HmacSha256Signer(def);
        }
    }
}
