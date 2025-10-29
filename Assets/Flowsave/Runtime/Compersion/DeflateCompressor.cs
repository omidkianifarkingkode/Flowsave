using System;
using System.IO;
using System.IO.Compression;

namespace Flowsave.Compersion
{
    /// <summary>
    /// Deflate (zlib raw) compressor using System.IO.Compression. Broadly available on Unity.
    /// </summary>
    public sealed class DeflateCompressor : ICompressor
    {
        public CompressionAlgId AlgId => CompressionAlgId.Deflate;
        public bool IsNoOp => false;


        public byte[] Compress(ReadOnlySpan<byte> data)
        {
            using var ms = new MemoryStream();
            using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                ds.Write(data);
            }
            return ms.ToArray();
        }


        public byte[] Decompress(ReadOnlySpan<byte> data)
        {
            using var input = new MemoryStream(data.ToArray());
            using var ds = new DeflateStream(input, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            ds.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
