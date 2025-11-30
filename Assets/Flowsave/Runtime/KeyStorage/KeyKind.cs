using System;
using UnityEngine;
using FlowSave.Encryption;
using FlowSave.Signing;

namespace FlowSave.KeyStorage
{
    public enum KeyKind
    {
        Aes,
        Hmac
    }

    [Serializable]
    public class KeyDefinition
    {
        [Tooltip("Logical identifier, e.g. 'aes-main' or 'hmac-main'.")]
        public string KeyId = "main";

        [Tooltip("How this key is used (AES encryption or HMAC signing).")]
        public KeyKind Kind = KeyKind.Aes;

        [Tooltip("AES key size (only used when Kind = Aes).")]
        public KeyBits KeyBits = KeyBits._128;

        [Tooltip("Truncate HMAC output (only used when Kind = Hmac).")]
        public HmacTruncate TruncateTo = HmacTruncate.None;

        [Tooltip("If true, derive per user/device using KeyResolver.")]
        public bool DeriveKey = false;

        [Tooltip("Base64 of raw key bytes. TEST ONLY – do not ship real keys.")]
        public string KeyB64 = string.Empty;

        public static KeyDefinition Clone(KeyDefinition from) =>
            from == null ? null : new KeyDefinition
            {
                KeyId = from.KeyId,
                Kind = from.Kind,
                KeyBits = from.KeyBits,
                TruncateTo = from.TruncateTo,
                DeriveKey = from.DeriveKey,
                KeyB64 = from.KeyB64
            };
    }
}
