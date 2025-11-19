using System;

namespace Flowsave.Compression
{
    public sealed class NoOpCompressor : ICompressor
    {
        public CompressionType AlgId => CompressionType.None;
        public bool IsNoOp => true;
        public byte[] Compress(ReadOnlySpan<byte> data) => data.ToArray();
        public byte[] Decompress(ReadOnlySpan<byte> data) => data.ToArray();
    }
}
