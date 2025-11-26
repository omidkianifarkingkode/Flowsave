using System;

namespace Flowsave.Operations
{
    public interface ISigner
    {
        SigningType Alg { get; }
        bool IsNoOp { get; }

        /// <summary>Wraps payload and signature into a single envelope.</summary>
        Result<byte[]> Sign(byte[] payload);

        /// <summary>Validates signature and returns the original payload on success.</summary>
        Result<byte[]> Verify(byte[] signedEnvelope);
    }
}
