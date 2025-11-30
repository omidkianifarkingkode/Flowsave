using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlowSave.Storage
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StoragePathRoot
    {
        ProjectRoot,         // <Project>/
        PersistentDataPath,  // Application.persistentDataPath
        DataPath,            // Application.dataPath
        TemporaryCachePath,  // Application.temporaryCachePath
        Absolute             // treat 'path' as absolute
    }
}