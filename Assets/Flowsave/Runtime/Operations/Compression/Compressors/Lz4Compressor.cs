#if FLOWSAVE_LZ4

using System;

namespace Flowsave.Compression
{
    /// <summary>
    /// LZ4 compressor wrapper. Requires K4os.Compression.LZ4 if you want to use it.
    /// Define FLOWSAVE_LZ4 and add the package to enable.
    /// </summary>
    public sealed class Lz4Compressor : ICompressor
    {
        public CompressionType AlgId => CompressionType.LZ4;
        public bool IsNoOp => false;

        public Result<byte[]> Compress(byte[] data)
        {
            if (data == null)
                return Result<byte[]>.Failure("Data is null.");

            try
            {
                // K4os API already takes byte[]
                var compressed = K4os.Compression.LZ4.LZ4Pickler.Pickle(data);
                return Result<byte[]>.Success(compressed);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }

        public Result<byte[]> Decompress(byte[] data)
        {
            if (data == null)
                return Result<byte[]>.Failure("Data is null.");

            try
            {
                var decompressed = K4os.Compression.LZ4.LZ4Pickler.Unpickle(data);
                return Result<byte[]>.Success(decompressed);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }
    }
}

#endif
