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


        [Tooltip("Base64 of raw AES key bytes. TEST ONLY – do not ship real keys.")]
        public string KeyB64 = string.Empty;


        [Tooltip("Nonce strategy for AES-GCM. Random is recommended.")]
        public NonceStrategy Nonce = NonceStrategy.Random;


        [Tooltip("GCM tag length in bytes. 16 (128-bit) is recommended.")]
        public TagBytes TagBytes = TagBytes._16;

        public byte[] Key
        {
            get
            {
                if (string.IsNullOrEmpty(KeyB64)) return Array.Empty<byte>();
                return Convert.FromBase64String(KeyB64);
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
                KeyB64 = from.KeyB64,
                Nonce = from.Nonce,
                TagBytes = from.TagBytes
            };

    }
}
