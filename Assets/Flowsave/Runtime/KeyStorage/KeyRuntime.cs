using FlowSave.Logging;
using FlowSave.Signing;
using System;

namespace FlowSave.KeyStorage
{
    public static class KeyRuntime
    {
        public static byte[] ResolveAesKey(KeyDefinition def)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));
            if (def.Kind != KeyKind.Aes)
                throw new ArgumentException("KeyDefinition.Kind must be Aes.", nameof(def));

            if (string.IsNullOrEmpty(def.KeyB64))
                return Array.Empty<byte>();

            byte[] baseKey;
            try
            {
                baseKey = Convert.FromBase64String(def.KeyB64);
            }
            catch (FormatException)
            {
                FlowSaveLog.Warning("[KeyRuntime] AES KeyB64 is not valid Base64.");
                return Array.Empty<byte>();
            }

            if (baseKey.Length == 0)
                return baseKey;

            if (!def.DeriveKey)
                return baseKey;

            // reuse your existing KeyResolver
            return KeyResolver.DeriveAesKey(baseKey, def.KeyBits);
        }

        public static byte[] ResolveHmacKey(KeyDefinition def, out string keyId, out int truncateBytes)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));
            if (def.Kind != KeyKind.Hmac)
                throw new ArgumentException("KeyDefinition.Kind must be Hmac.", nameof(def));

            keyId = def.KeyId ?? string.Empty;
            truncateBytes = def.TruncateTo == HmacTruncate.None
                ? 0
                : (int)def.TruncateTo;

            if (string.IsNullOrEmpty(def.KeyB64))
                return Array.Empty<byte>();

            byte[] baseKey;
            try
            {
                baseKey = Convert.FromBase64String(def.KeyB64);
            }
            catch (FormatException)
            {
                FlowSaveLog.Warning("[KeyRuntime] HMAC KeyB64 is not valid Base64.");
                return Array.Empty<byte>();
            }

            if (baseKey.Length == 0)
                return baseKey;

            if (!def.DeriveKey)
                return baseKey;

            return KeyResolver.DeriveHmacKey(baseKey, 32);
        }
    }
}
