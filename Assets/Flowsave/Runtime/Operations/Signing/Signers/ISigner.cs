namespace FlowSave.Signing
{
    public interface ISigner
    {
        SigningType Alg { get; }
        bool IsNoOp { get; }
        string KeyId { get; }

        /// <summary>
        /// Wraps payload and signature into a single envelope (legacy / raw usage).
        /// </summary>
        Result<byte[]> Sign(byte[] payload);

        /// <summary>
        /// Validates signature and returns the original payload on success (for legacy format).
        /// </summary>
        Result<byte[]> Verify(byte[] signedEnvelope);

        /// <summary>
        /// Computes a detached signature (MAC) for the given payload.
        /// Payload is NOT modified.
        /// </summary>
        Result<byte[]> ComputeSignature(byte[] payload);

        /// <summary>
        /// Verifies a detached signature for the given payload.
        /// Returns Success on valid signature, Failure otherwise.
        /// </summary>
        Result VerifySignature(byte[] payload, byte[] signature);
    }
}
