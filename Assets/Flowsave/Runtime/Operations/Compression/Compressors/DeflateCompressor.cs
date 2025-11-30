using System;
using System.IO;
using FlowSave.Compression;
using System.IO.Compression;

namespace FlowSave.Compression
{
    /// <summary>
    /// Deflate (zlib raw) compressor using System.IO.Compression. Broadly available on Unity.
    /// </summary>
    public sealed class DeflateCompressor : ICompressor
    {
        public CompressionType AlgId => CompressionType.Deflate;
        public bool IsNoOp => false;


        public Result<byte[]> Compress(byte[] data)
        {
            if (data == null) return CompressionErrors.DataNull;

            try
            {
                using var ms = new MemoryStream();
                using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                {
                    ds.Write(data, 0, data.Length);
                }
                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return CompressionErrors.CompressFailed(ex.Message, CompressionErrors.DeflateModule);
            }
        }

        public Result<byte[]> Decompress(byte[] data)
        {
            if (data == null) return CompressionErrors.DataNull;

            try
            {
                using var input = new MemoryStream(data);
                using var ds = new DeflateStream(input, CompressionMode.Decompress);
                using var ms = new MemoryStream();
                ds.CopyTo(ms);
                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return CompressionErrors.DecompressFailed(ex.Message, CompressionErrors.DeflateModule);
            }
        }
    }
}
