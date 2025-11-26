using System;
using System.IO;
using System.IO.Compression;

namespace Flowsave.Compression
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
            if (data == null) return Result<byte[]>.Failure("Data is null.");

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
                return Result<byte[]>.Failure(ex.Message);
            }
        }

        public Result<byte[]> Decompress(byte[] data)
        {
            if (data == null) return Result<byte[]>.Failure("Data is null.");

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
                return Result<byte[]>.Failure(ex.Message);
            }
        }
    }
}
