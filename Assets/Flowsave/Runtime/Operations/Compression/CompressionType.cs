using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Compression
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CompressionType : byte
    {
        None = 0,
        Deflate = 1,
        Brotli = 2,
#if !FLOWSAVE_LZ4
        LZ4 = 3,
#endif
    }
}
