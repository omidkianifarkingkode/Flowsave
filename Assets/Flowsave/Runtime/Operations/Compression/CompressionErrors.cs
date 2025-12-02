namespace FlowSave.Compression
{
    internal static class CompressionErrors
    {
        private const string CompressionPrefix = "[FlowSave]:compression:";
        private const string BrotliPrefix = "[FlowSave]:BrotliCompressor:";
        private const string DeflatePrefix = "[FlowSave]:DeflateCompressor:";
        private const string Lz4Prefix = "[FlowSave]:Lz4Compressor:";
        private const string NoOpPrefix = "[FlowSave]:NoOpCompressor:";

        public static readonly Result<byte[]> DataNull = Result<byte[]>.Failure($"{CompressionPrefix}Data is null.");

        public static Result<byte[]> CompressFailed(string message, string prefix = null)
            => Result<byte[]>.Failure($"{prefix ?? CompressionPrefix}Compress failed: {message}");

        public static Result<byte[]> DecompressFailed(string message, string prefix = null)
            => Result<byte[]>.Failure($"{prefix ?? CompressionPrefix}Decompress failed: {message}");

        public static string BrotliModule => BrotliPrefix;
        public static string DeflateModule => DeflatePrefix;
        public static string Lz4Module => Lz4Prefix;
        public static string NoOpModule => NoOpPrefix;
    }
}
