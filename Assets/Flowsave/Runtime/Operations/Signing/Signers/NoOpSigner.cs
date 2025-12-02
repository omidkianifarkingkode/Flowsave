using System;

namespace FlowSave.Signing
{
    /// <summary>
    /// No-op signer. Does not sign or wrap data in any way.
    /// Sign/Verify are simple passthroughs.
    /// Detached signature methods are also no-op.
    /// </summary>
    public sealed class NoOpSigner : ISigner
    {
        public SigningType Alg => SigningType.None;
        public bool IsNoOp => true;
        public string KeyId => string.Empty;

        // --------------------------------------------------------------------
        // Legacy envelope-style APIs
        // --------------------------------------------------------------------
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

        // --------------------------------------------------------------------
        // Detached signature APIs
        // --------------------------------------------------------------------
        public Result<byte[]> ComputeSignature(byte[] payload)
        {
            if (payload == null)
                return Result<byte[]>.Failure("Payload is null.");

            // No signature, indicate "nothing" with empty array
            return Result<byte[]>.Success(Array.Empty<byte>());
        }

        public Result VerifySignature(byte[] payload, byte[] signature)
        {
            if (payload == null)
                return Result.Failure("Payload is null.");

            // For no-op signer we accept everything as "valid"
            return Result.Success();
        }
    }
}
