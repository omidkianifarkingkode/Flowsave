using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Operations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NonceStrategy : byte
    {
        Random = 0,
        Counter = 1,
        Deterministic = 2,
    }
}
