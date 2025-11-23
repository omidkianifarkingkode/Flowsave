using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Serialization
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SerializationType
    {
        None = 0,
        Json = 1,
        Binary_Legacy = 2,
        Binary_Protobuf = 3,
        Binary_MessagePack = 4,
        Xml = 5,
        Csv = 6,
        Custom = 7
    }

}