using System;

namespace Flowsave.Compersion
{

    public interface ICompressor
    {
        byte[] Compress(ReadOnlySpan<byte> data);
        byte[] Decompress(ReadOnlySpan<byte> data);
        CompressionAlgId AlgId { get; }
        bool IsNoOp { get; }
    }
}
