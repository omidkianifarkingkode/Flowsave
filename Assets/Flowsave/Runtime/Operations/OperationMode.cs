using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace FlowSave.Operations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperationMode : byte
    {
        None = 0,
        Compression = 1,
        Encrypt = 2,
        Sign = 3,
        Checksum = 4,
    }
}