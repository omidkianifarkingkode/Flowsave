using FlowSave.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowSave.KeyStorage
{
    [Serializable]
    public class KeyStoreOptions
    {
        [Header("All keys for this environment")]
        public List<KeyDefinition> Keys = new();

        // Global default ids (not per-instance)
        public static string DefaultAesKeyId = "aes-main";
        public static string DefaultHmacKeyId = "hmac-main";

        public static KeyStoreOptions Clone(KeyStoreOptions from)
        {
            if (from == null)
                return null;

            var clone = new KeyStoreOptions
            {
                Keys = new List<KeyDefinition>(from.Keys?.Count ?? 0)
            };

            if (from.Keys != null)
            {
                for (int i = 0; i < from.Keys.Count; i++)
                    clone.Keys.Add(KeyDefinition.Clone(from.Keys[i]));
            }

            return clone;
        }

        // -----------------------------
        // Internal helpers
        // -----------------------------

        private KeyDefinition FindKey(string keyId, KeyKind kind, string defaultId)
        {
            if (Keys == null || Keys.Count == 0)
                return null;

            if (string.IsNullOrWhiteSpace(keyId))
                keyId = defaultId;

            // Prefer exact id + kind match
            for (int i = 0; i < Keys.Count; i++)
            {
                var k = Keys[i];
                if (k != null && k.KeyId == keyId && k.Kind == kind)
                    return k;
            }

            // Fallback: first key of that kind
            for (int i = 0; i < Keys.Count; i++)
            {
                var k = Keys[i];
                if (k != null && k.Kind == kind)
                    return k;
            }

            FlowSaveLog.Warning($"[KeyStore] No key of kind {kind} found (id '{keyId}').");
            return null;
        }

        public KeyDefinition GetAesDefinition(string keyId = null) =>
            FindKey(keyId, KeyKind.Aes, DefaultAesKeyId);

        public KeyDefinition GetHmacDefinition(string keyId = null) =>
            FindKey(keyId, KeyKind.Hmac, DefaultHmacKeyId);
    }
}
