using System;

namespace Flowsave.Checksum
{
    public sealed class NoOpChecksum : IChecksum
    {
        public ChecksumAlgId Alg => ChecksumAlgId.None;
        public bool IsNoOp => true;
        public byte[] Compute(ReadOnlySpan<byte> message) => Array.Empty<byte>();
        public bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> digest) => true;
    }
}