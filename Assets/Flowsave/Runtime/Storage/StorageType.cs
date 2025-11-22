using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Storage
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StorageType
    {
        FileSystem,
        PlayerPrefs,
        Cloud,
        Custom
    }

}