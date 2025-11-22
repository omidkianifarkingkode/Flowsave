using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Flowsave.Storage
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