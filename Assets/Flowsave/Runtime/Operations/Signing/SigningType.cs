using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Operations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SigningType : byte
    {
        None = 0,
        Hmac = 1,
    }
}
