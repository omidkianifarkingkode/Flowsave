#if FLOWSAVE_LZ4

using System;

namespace Flowsave.Compression
{
    /// <summary>
    /// LZ4 compressor wrapper. Requires K4os.Compression.LZ4 if you want to use it.
    /// Define K4OS_LZ4 and add the package to enable. Otherwise, this will throw when used.
    /// </summary>
    public sealed class Lz4Compressor : ICompressor
    {
        public CompressionType AlgId => CompressionType.LZ4;
        public bool IsNoOp => false;


        public byte[] Compress(ReadOnlySpan<byte> data)
        {
            return K4os.Compression.LZ4.LZ4Pickler.Pickle(data.ToArray());
        }


        public byte[] Decompress(ReadOnlySpan<byte> data)
        {
            return K4os.Compression.LZ4.LZ4Pickler.Unpickle(data.ToArray());
        }
    }
}
#endif