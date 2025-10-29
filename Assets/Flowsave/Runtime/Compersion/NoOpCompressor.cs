using System;

namespace Flowsave.Compersion
{
    public sealed class NoOpCompressor : ICompressor
    {
        public CompressionAlgId AlgId => CompressionAlgId.None;
        public bool IsNoOp => true;
        public byte[] Compress(ReadOnlySpan<byte> data) => data.ToArray();
        public byte[] Decompress(ReadOnlySpan<byte> data) => data.ToArray();
    }
}
