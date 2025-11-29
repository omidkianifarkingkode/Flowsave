using System;
using UnityEngine;

namespace Flowsave.Operations
{
    public enum KeyBits { _128  = 128, _256 = 256 }
    public enum TagBytes { _12 = 12, _13 = 13, _14 = 14, _15 = 15, _16 = 16 }

    [Serializable]
    public class AesOptions
    {
        [Tooltip("AES key size. Only 128 or 256 are valid.")]
        public KeyBits KeyBits = KeyBits._128;

        [Tooltip("If true, the runtime AES key is derived per user/device from the base key.")]
        public bool DeriveKey = false;

        [Tooltip("Base64 of raw AES key bytes. TEST ONLY – do not ship real keys.")]
        public string KeyB64 = string.Empty;

        /// <summary>
        /// Base key as stored in config (no per-user derivation).
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
                    Debug.LogWarning("[AesOptions] KeyB64 is not valid Base64.");
                    return Array.Empty<byte>();
                }
            }
        }

        /// <summary>
        /// Runtime key to actually use.
        /// - If DerivePerUserKey is false or KeyResolver is null: returns BaseKey.
        /// - Otherwise: returns KeyResolver(BaseKey, KeyBits).
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

                var resolved = KeyResolver.DeriveAesKey(baseKey, KeyBits);
                if (resolved == null || resolved.Length == 0)
                {
                    Debug.LogWarning("[AesOptions] KeyResolver returned null/empty key, falling back to BaseKey.");
                    return baseKey;
                }

                return resolved;
            }
        }


        public void Validate()
        {
            if (Key.Length != ((int)KeyBits / 8))
                Debug.LogWarning($"[AesOptions] key length is {Key.Length} bytes but keyBits is {KeyBits}. For tests it will still run but fix this before shipping.");
        }

        public static AesOptions Clone(AesOptions from) =>
            from == null ? null : new AesOptions
            {
                KeyBits = from.KeyBits,
                DeriveKey = from.DeriveKey,
                KeyB64 = from.KeyB64
            };

    }
}
