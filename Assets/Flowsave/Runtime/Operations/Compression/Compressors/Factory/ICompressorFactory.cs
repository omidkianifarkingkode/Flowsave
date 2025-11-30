namespace FlowSave.Compression
{
    public interface ICompressorFactory
    {
        public ICompressor CreateCompressor(CompressionType compressionAlg);
    }
}
