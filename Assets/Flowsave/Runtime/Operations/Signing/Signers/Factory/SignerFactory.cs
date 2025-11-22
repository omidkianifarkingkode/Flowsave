using Flowsave.Operations.Options;
using System;

namespace Flowsave.Operations
{
    public class SignerFactory : ISignerFactory
    {
        private readonly SigningOptions _options;

        public SignerFactory(SigningOptions options)
        {
            _options = options;
        }

        public ISigner CreateSigner(SigningType signAlg)
        {
            return signAlg switch
            {
                SigningType.None => new NoOpSigner(),
                SigningType.Hmac => new HmacSha256Signer(_options.Hmac),
                _ => throw new InvalidOperationException($"Unsupported signing algorithm: {signAlg}")
            };
        }
    }
}
