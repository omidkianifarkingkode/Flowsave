using System;

namespace Flowsave.Operations
{
    /// <summary>
    /// No-op signer. Does not sign or wrap data in any way.
    /// Sign/Verify are simple passthroughs.
    /// </summary>
    public sealed class NoOpSigner : ISigner
    {
        public SigningType Alg => SigningType.None;
        public bool IsNoOp => true;

        public Result<byte[]> Sign(byte[] payload)
        {
            if (payload == null)
                return Result<byte[]>.Failure("Payload is null.");

            // No signing, no envelope – just return input as-is
            return Result<byte[]>.Success(payload);
        }

        public Result<byte[]> Verify(byte[] signedEnvelope)
        {
            if (signedEnvelope == null)
                return Result<byte[]>.Failure("Signed envelope is null.");

            // Nothing to verify – just treat input as already-verified payload
            return Result<byte[]>.Success(signedEnvelope);
        }
    }
}
