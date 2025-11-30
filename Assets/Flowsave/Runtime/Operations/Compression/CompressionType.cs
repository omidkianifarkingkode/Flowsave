using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlowSave.Compression
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CompressionType : byte
    {
        None = 0,
        Deflate = 1,
        Brotli = 2,
        LZ4 = 3,
    }
}
