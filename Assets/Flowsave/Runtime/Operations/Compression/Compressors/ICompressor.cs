using System;

namespace Flowsave.Compression
{

    public interface ICompressor
    {
        byte[] Compress(ReadOnlySpan<byte> data);
        byte[] Decompress(ReadOnlySpan<byte> data);
        CompressionType AlgId { get; }
        bool IsNoOp { get; }
    }
}
