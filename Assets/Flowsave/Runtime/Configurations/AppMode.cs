using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Flowsave.Namespaces
{
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AppMode : byte
    {
        None = 0,
        Editor = 1 << 0,        // 1
        Development = 1 << 1,   // 2
        Release = 1 << 2,       // 4
    }
}