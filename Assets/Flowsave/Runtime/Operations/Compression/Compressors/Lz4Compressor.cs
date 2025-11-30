#if FLOWSAVE_LZ4

using System;
using LZ4;

namespace FlowSave.Compression
{
    /// <summary>
    /// LZ4 compressor wrapper. Requires lz4net (LZ4.LZ4Codec) if you want to use it.
    /// Define FLOWSAVE_LZ4 and add the package to enable.
    /// </summary>
    public sealed class Lz4Compressor : ICompressor
    {
        public CompressionType AlgId => CompressionType.LZ4;
        public bool IsNoOp => false;

        public Result<byte[]> Compress(byte[] data)
        {
            if (data == null)
                return CompressionErrors.DataNull;

            try
            {
                // Wrap adds a small header with original length etc.,
                var compressed = LZ4Codec.Wrap(data);
                return Result<byte[]>.Success(compressed);
            }
            catch (Exception ex)
            {
                return CompressionErrors.CompressFailed(ex.Message, CompressionErrors.Lz4Module);
            }
        }

        public Result<byte[]> Decompress(byte[] data)
        {
            if (data == null)
                return CompressionErrors.DataNull;

            try
            {
                // UnWrap uses the header produced by Wrap and restores original bytes.
                var decompressed = LZ4Codec.Unwrap(data);
                return Result<byte[]>.Success(decompressed);
            }
            catch (Exception ex)
            {
                return CompressionErrors.DecompressFailed(ex.Message, CompressionErrors.Lz4Module);
            }
        }
    }
}

#endif
