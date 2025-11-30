namespace FlowSave.Compression
{
    public sealed class NoOpCompressor : ICompressor
    {
        public CompressionType AlgId => CompressionType.None;
        public bool IsNoOp => true;

        public Result<byte[]> Compress(byte[] data)
        {
            // No copying—just return the same reference.
            if (data == null)
                return Result<byte[]>.Failure("Data is null.");

            return Result<byte[]>.Success(data);
        }

        public Result<byte[]> Decompress(byte[] data)
        {
            if (data == null)
                return Result<byte[]>.Failure("Data is null.");

            return Result<byte[]>.Success(data);
        }
    }
}
