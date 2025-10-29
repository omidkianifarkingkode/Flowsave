using System;

namespace Flowsave.Compersion
{
    /// <summary>
    /// LZ4 compressor wrapper. Requires K4os.Compression.LZ4 if you want to use it.
    /// Define K4OS_LZ4 and add the package to enable. Otherwise, this will throw when used.
    /// </summary>
    public sealed class Lz4Compressor : ICompressor
    {
        public CompressionAlgId AlgId => CompressionAlgId.LZ4;
        public bool IsNoOp => false;


        public byte[] Compress(ReadOnlySpan<byte> data)
        {
#if K4OS_LZ4
return K4os.Compression.LZ4.LZ4Pickler.Pickle(data.ToArray());
#else
            throw new NotSupportedException("LZ4 requires K4os.Compression.LZ4. Define K4OS_LZ4 and add the package.");
#endif
        }


        public byte[] Decompress(ReadOnlySpan<byte> data)
        {
#if K4OS_LZ4
return K4os.Compression.LZ4.LZ4Pickler.Unpickle(data.ToArray());
#else
            throw new NotSupportedException("LZ4 requires K4os.Compression.LZ4. Define K4OS_LZ4 and add the package.");
#endif
        }
    }
}
