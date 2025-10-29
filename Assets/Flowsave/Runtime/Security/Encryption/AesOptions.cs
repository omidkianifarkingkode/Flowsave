using System;
using UnityEngine;

namespace Flowsave.Security
{
    [Serializable]
    public class AesOptions
    {
        [Tooltip("AES key size. Only 128 or 256 are valid.")]
        public int keyBits = 256; // 128 or 256


        [Tooltip("Base64 of raw AES key bytes. TEST ONLY – do not ship real keys.")]
        public string keyB64 = string.Empty;


        [Tooltip("Nonce strategy for AES-GCM. Random is recommended.")]
        public NonceStrategy nonce = NonceStrategy.Random;


        [Tooltip("GCM tag length in bytes. 16 (128-bit) is recommended.")]
        [Range(12, 16)] public int tagBytes = 16;


        public byte[] Key
        {
            get
            {
                if (string.IsNullOrEmpty(keyB64)) return Array.Empty<byte>();
                return Convert.FromBase64String(keyB64);
            }
        }


        public void Validate()
        {
            if (keyBits != 128 && keyBits != 256)
                throw new ArgumentOutOfRangeException(nameof(keyBits), "AES keyBits must be 128 or 256.");
            if (tagBytes < 12 || tagBytes > 16)
                throw new ArgumentOutOfRangeException(nameof(tagBytes), "GCM tag must be 12..16 bytes (16 recommended).");
            if (Key.Length != (keyBits / 8))
                Debug.LogWarning($"[AesOptions] key length is {Key.Length} bytes but keyBits is {keyBits}. For tests it will still run but fix this before shipping.");
        }
    }
}
