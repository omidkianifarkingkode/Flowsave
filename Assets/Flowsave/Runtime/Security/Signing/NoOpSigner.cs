using System;

namespace Flowsave.Security
{
    /// <summary>
    /// No-op signer for scenarios where signing is disabled but interface is required.
    /// </summary>
    public sealed class NoOpSigner : ISigner
    {
        public SigAlgId Alg => SigAlgId.None;
        public string SignerId => string.Empty;
        public bool IsNoOp => true;
        public byte[] Sign(ReadOnlySpan<byte> message) => Array.Empty<byte>();
        public bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, string signerId) => true;
    }
}
