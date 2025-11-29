using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Operations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EncryptionType : byte
    {
        None = 0,
        Aes128Cbc = 1,
        Aes256Cbc = 2
    }
}
