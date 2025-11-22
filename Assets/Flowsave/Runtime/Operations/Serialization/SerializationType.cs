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
#if FLOWSAVE_PROTOBUF_NET
        Binary_Protobuf = 3,
#endif
#if FLOWSAVE_MessagePack
        Binary_MessagePack = 4,
#endif
        Xml = 5,
        Csv = 6,
        Custom = 7
    }

}