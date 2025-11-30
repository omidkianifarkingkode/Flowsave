using System;
using FlowSave.Logging;
using UnityEngine;

namespace FlowSave.Signing
{
    public enum HmacTruncate { None = 0, _16 = 16, _32 = 32 }

    [Serializable]
    public class HmacOptions
    {
        [Tooltip("If true, the runtime HMAC key is derived per user/device from the base key.")]
        public bool DeriveKey = false;

        [Tooltip("Base64 of HMAC key bytes (e.g., 32 bytes recommended). TEST ONLY – do not ship real keys.")]
        public string KeyB64 = string.Empty;


        [Tooltip("Identifier for this HMAC key (used in envelopes/headers for rotation). Not secret.")]
        public string KeyId = "hmac-test";


        [Tooltip("Truncate HMAC output to N bytes (leave 0 for full 32). 16..32 typical. Lower = smaller but weaker.")]
        public HmacTruncate TruncateTo = HmacTruncate.None;


        /// <summary>
        /// Raw key stored in config (no derivation).
        /// </summary>
        public byte[] BaseKey
        {
            get
            {
                if (string.IsNullOrEmpty(KeyB64))
                    return Array.Empty<byte>();

                try
                {
                    return Convert.FromBase64String(KeyB64);
                }
                catch (FormatException)
                {
                    FlowSaveLog.Warning("[HmacOptions] KeyB64 is not valid Base64.");
                    return Array.Empty<byte>();
                }
            }
        }

        /// <summary>
        /// Final key used at runtime:
        /// - If DerivePerUserKey = false OR no resolver set → BaseKey
        /// - Otherwise → KeyResolver(BaseKey, desiredLength)
        ///
        /// NOTE: full HMAC-SHA256 key length is 32 bytes normally.
        /// </summary>
        public byte[] Key
        {
            get
            {
                var baseKey = BaseKey;
                if (baseKey.Length == 0)
                    return baseKey;

                if (!DeriveKey)
                    return baseKey;

                int desired = 32; // canonical HMAC-SHA256 key length
                var resolved = KeyResolver.DeriveHmacKey(baseKey, desired);

                if (resolved == null || resolved.Length == 0)
                {
                    FlowSaveLog.Warning("[HmacOptions] KeyResolver returned null/empty key, falling back to BaseKey.");
                    return baseKey;
                }

                return resolved;
            }
        }

        public static HmacOptions Clone(HmacOptions from) =>
            from == null ? null : new HmacOptions
            {
                DeriveKey = from.DeriveKey,
                KeyB64 = from.KeyB64,
                KeyId = from.KeyId,
                TruncateTo = from.TruncateTo
            };

    }
}
