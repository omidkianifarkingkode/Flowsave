using System;
using FlowSave.Compression;
using System.IO;
using System.IO.Compression;

namespace FlowSave.Compression
{
    /// <summary>
    /// Brotli compressor. Available on modern Unity/.NET. If your target doesn't have BrotliStream,
    /// either remove this class or add the System.IO.Compression.Brotli package.
    /// </summary>
    public sealed class BrotliCompressor : ICompressor
    {
        public CompressionType AlgId => CompressionType.Brotli;
        public bool IsNoOp => false;

        public Result<byte[]> Compress(byte[] data)
        {
            if (data == null)
                return CompressionErrors.DataNull;

            try
            {
                using var ms = new MemoryStream();
                using (var bs = new BrotliStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                {
                    bs.Write(data, 0, data.Length);
                }
                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return CompressionErrors.CompressFailed(ex.Message, CompressionErrors.BrotliModule);
            }
        }

        public Result<byte[]> Decompress(byte[] data)
        {
            if (data == null)
                return CompressionErrors.DataNull;

            try
            {
                // no extra ToArray() – MemoryStream can take the byte[] directly
                using var input = new MemoryStream(data, writable: false);
                using var bs = new BrotliStream(input, CompressionMode.Decompress);
                using var ms = new MemoryStream();
                bs.CopyTo(ms);
                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return CompressionErrors.DecompressFailed(ex.Message, CompressionErrors.BrotliModule);
            }
        }
    }
}
