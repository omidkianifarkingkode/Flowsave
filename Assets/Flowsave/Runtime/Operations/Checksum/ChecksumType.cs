using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Checksum
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChecksumType : byte
    {
        None = 0,
        CRC32C = 1,
        SHA256 = 2
    }
}