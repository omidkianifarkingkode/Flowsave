using System;
using UnityEngine;

namespace Flowsave.Operations.Options
{
    public enum HmacTruncate { None = 0, _16 = 16, _32 = 32 }

    [Serializable]
    public class HmacOptions
    {
        [Tooltip("Base64 of HMAC key bytes (e.g., 32 bytes recommended). TEST ONLY – do not ship real keys.")]
        public string KeyB64 = string.Empty;


        [Tooltip("Identifier for this HMAC key (used in envelopes/headers for rotation). Not secret.")]
        public string KeyId = "hmac-test";


        [Tooltip("Truncate HMAC output to N bytes (leave 0 for full 32). 16..32 typical. Lower = smaller but weaker.")]
        public HmacTruncate TruncateTo = HmacTruncate.None;


        public byte[] Key => string.IsNullOrEmpty(KeyB64) ? Array.Empty<byte>() : Convert.FromBase64String(KeyB64);


        public static HmacOptions Clone(HmacOptions from) =>
            from == null ? null : new HmacOptions
            {
                KeyB64 = from.KeyB64,
                KeyId = from.KeyId,
                TruncateTo = from.TruncateTo
            };

    }
}
