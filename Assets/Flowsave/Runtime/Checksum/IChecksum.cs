using System;

namespace Flowsave.Checksum
{
    public interface IChecksum
    {
        ChecksumAlgId Alg { get; }
        byte[] Compute(ReadOnlySpan<byte> message);
        bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> digest);
        bool IsNoOp { get; }
    }
}