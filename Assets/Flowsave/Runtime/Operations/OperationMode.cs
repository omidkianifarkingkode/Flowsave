using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Flowsave.Operations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperationMode : byte
    {
        None = 0,
        Compression,
        Sign,
        Checksum,
        Encrypt,
        ObfuscateName,
    }
}