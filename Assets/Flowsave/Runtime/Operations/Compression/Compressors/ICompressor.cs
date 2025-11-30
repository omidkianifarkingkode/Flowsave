namespace FlowSave.Compression
{
    public interface ICompressor
    {
        CompressionType AlgId { get; }
        bool IsNoOp { get; }

        Result<byte[]> Compress(byte[] data);
        Result<byte[]> Decompress(byte[] data);
    }
}
