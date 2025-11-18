using System;
using UnityEngine;

namespace Flowsave.Security
{
    [Serializable]
    public class AesOptions
    {
        [Tooltip("AES key size. Only 128 or 256 are valid.")]
        [field: SerializeField] public int KeyBits { get; private set; } = 256; // 128 or 256


        [Tooltip("Base64 of raw AES key bytes. TEST ONLY – do not ship real keys.")]
        [field: SerializeField] public string KeyB64 { get; private set; } = string.Empty;


        [Tooltip("Nonce strategy for AES-GCM. Random is recommended.")]
        [field: SerializeField] public NonceStrategy Nonce { get; private set; } = NonceStrategy.Random;


        [Tooltip("GCM tag length in bytes. 16 (128-bit) is recommended.")]
        [field: SerializeField] public int TagBytes { get; private set; } = 16;

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
            if (KeyBits != 128 && KeyBits != 256)
                throw new ArgumentOutOfRangeException(nameof(KeyBits), "AES keyBits must be 128 or 256.");
            if (TagBytes < 12 || TagBytes > 16)
                throw new ArgumentOutOfRangeException(nameof(TagBytes), "GCM tag must be 12..16 bytes (16 recommended).");
            if (Key.Length != (KeyBits / 8))
                Debug.LogWarning($"[AesOptions] key length is {Key.Length} bytes but keyBits is {KeyBits}. For tests it will still run but fix this before shipping.");
        }
    }
}
