using System;

namespace FlowSave.Checksum
{
    public sealed class NoOpChecksum : IChecksum
    {
        public ChecksumType Alg => ChecksumType.None;
        public bool IsNoOp => true;
        public byte[] Compute(ReadOnlySpan<byte> message) => Array.Empty<byte>();
        public bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> digest) => true;
    }
}