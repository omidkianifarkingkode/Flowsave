using System;
using UnityEngine;

namespace Flowsave.Security.Options
{
    [Serializable]
    public class HmacOptions
    {
        [Tooltip("Base64 of HMAC key bytes (e.g., 32 bytes recommended). TEST ONLY – do not ship real keys.")]
        [field: SerializeField] public string KeyB64 { get; private set; } = string.Empty;


        [Tooltip("Identifier for this HMAC key (used in envelopes/headers for rotation). Not secret.")]
        [field: SerializeField] public string KeyId { get; private set; } = "hmac-test";


        [Tooltip("Truncate HMAC output to N bytes (leave 0 for full 32). 16..32 typical. Lower = smaller but weaker.")]
        [field: SerializeField] public int TruncateTo { get; private set; } = 0;


        public byte[] Key => string.IsNullOrEmpty(KeyB64) ? Array.Empty<byte>() : Convert.FromBase64String(KeyB64);


        public void Validate()
        {
            if (TruncateTo != 0 && (TruncateTo < 10 || TruncateTo > 32))
                Debug.LogWarning("[HmacOptions] Truncation outside 10..32 bytes is unusual. Use 16..32 typically, or 0 for full.");
        }
    }
}
