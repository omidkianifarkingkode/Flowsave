using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Operations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EncryptionType : byte
    {
        None = 0,
        Aes128Gcm = 1,
        Aes256Gcm = 2
    }
}
