using FlowSave.Operations;
using System.Collections.Generic;

namespace FlowSave.Codec
{
    public sealed class OperationDescriptor
    {
        // e.g. "compress", "encrypt"
        public OperationMode Kind { get; set; }

        // e.g. "lz4", "aes-gcm"
        public string AlgorithmId { get; set; }

        // e.g. "aes-main", null if not key-based
        public string KeyId { get; set; }

        // optional algorithm-specific stuff (iv, nonce, etc.)
        public Dictionary<string, string> Parameters { get; set; }
    }
}
