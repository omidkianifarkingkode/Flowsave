using System;

namespace FlowSave.Checksum
{
    public interface IChecksum
    {
        ChecksumType Alg { get; }
        byte[] Compute(ReadOnlySpan<byte> message);
        bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> digest);
        bool IsNoOp { get; }
    }
}