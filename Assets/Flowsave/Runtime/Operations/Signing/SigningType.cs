using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlowSave.Signing
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SigningType : byte
    {
        None = 0,
        Hmac = 1,
    }
}
