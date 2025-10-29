using System;

namespace Flowsave.Security
{
    public interface ISigner
    {
        SigAlgId Alg { get; }
        string SignerId { get; } // key ID or account ID
        byte[] Sign(ReadOnlySpan<byte> message);
        bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, string signerId);
        bool IsNoOp { get; }
    }
}
