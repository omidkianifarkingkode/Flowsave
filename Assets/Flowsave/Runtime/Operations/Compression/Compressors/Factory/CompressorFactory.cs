namespace Flowsave.Compression
{
    public class CompressorFactory : ICompressorFactory
    {
        public ICompressor CreateCompressor(CompressionType compressionType)
        {
            return compressionType switch
            {
                CompressionType.None => new NoOpCompressor(),
                CompressionType.Deflate => new DeflateCompressor(),
                CompressionType.Brotli => new BrotliCompressor(),
#if FLOWSAVE_LZ4
                CompressionAlgId.LZ4 => new Lz4Compressor(),
#endif
                _ => throw new System.ArgumentOutOfRangeException(nameof(compressionType), "Unsupported compression algorithm."),
            };
        }
    }
}
