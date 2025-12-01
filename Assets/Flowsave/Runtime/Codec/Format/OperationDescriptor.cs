using System.Collections.Generic;

namespace Flowsave.Codec
{
    public sealed class OperationDescriptor
    {
        // e.g. "compress", "encrypt"
        public string Kind { get; set; }

        // e.g. "lz4", "aes-gcm"
        public string AlgorithmId { get; set; }

        // e.g. "aes-main", null if not key-based
        public string KeyId { get; set; }

        // optional algorithm-specific stuff (iv, nonce, etc.)
        public Dictionary<string, string> Parameters { get; set; }
    }
}
