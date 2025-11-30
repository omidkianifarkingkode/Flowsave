using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlowSave.Storage
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