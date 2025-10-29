using System;
using System.IO;
using System.IO.Compression;

namespace Flowsave.Compersion
{
    /// <summary>
    /// Brotli compressor. Available on modern Unity/.NET. If your target doesn't have BrotliStream,
    /// either remove this class or add the System.IO.Compression.Brotli package.
    /// </summary>
    public sealed class BrotliCompressor : ICompressor
    {
        public CompressionAlgId AlgId => CompressionAlgId.Brotli;
        public bool IsNoOp => false;


        public byte[] Compress(ReadOnlySpan<byte> data)
        {
            using var ms = new MemoryStream();
            using (var bs = new BrotliStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                bs.Write(data);
            }
            return ms.ToArray();
        }


        public byte[] Decompress(ReadOnlySpan<byte> data)
        {
            using var input = new MemoryStream(data.ToArray());
            using var bs = new BrotliStream(input, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            bs.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
