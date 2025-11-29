using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Flowsave.Operations;
using Flowsave.Operations.Options;

namespace Flowsave
{
    /// <summary>
    /// Holds runtime identity (user / device) and resolves per-user keys
    /// from base keys defined in FlowSaveConfiguration.
    /// </summary>
    public static class KeyResolver
    {
        /// <summary>
        /// Current device id. Default is Unity's deviceUniqueIdentifier.
        /// </summary>
        public static string UniqueId { get; private set; } =
            SystemInfo.deviceUniqueIdentifier ?? "unknown-device";

        /// <summary>
        /// Initialize identity. Call once at bootstrap when you know userId.
        /// </summary>
        public static void Initialize(string uniqueId = default)
        {
            if (!string.IsNullOrWhiteSpace(uniqueId))
                UniqueId = uniqueId.Trim();
            else if (!string.IsNullOrEmpty(SystemInfo.deviceUniqueIdentifier))
                UniqueId = SystemInfo.deviceUniqueIdentifier;
        }

        // ------------------------------------------------------------
        // AES HELPERS
        // ------------------------------------------------------------

        /// <summary>
        /// Default AES key derivation: HMAC-SHA256(baseKey, user|device) truncated to KeyBits size.
        /// </summary>
        public static byte[] DeriveAesKey(byte[] baseKey, KeyBits bits)
        {
            if (baseKey == null || baseKey.Length == 0)
                return Array.Empty<byte>();

            int outputBytes = ((int)bits) / 8;
            if (outputBytes <= 0)
                return Array.Empty<byte>();

            string id = UniqueId;
            byte[] idBytes = Encoding.UTF8.GetBytes(id);

            byte[] full;
            using (var hmac = new HMACSHA256(baseKey))
            {
                full = hmac.ComputeHash(idBytes);
            }

            if (full.Length == outputBytes)
                return full;

            var result = new byte[outputBytes];
            Buffer.BlockCopy(full, 0, result, 0, Math.Min(full.Length, outputBytes));
            return result;
        }


        // ------------------------------------------------------------
        // HMAC HELPERS
        // ------------------------------------------------------------

        /// <summary>
        /// Derive HMAC key from base key for this device/user.
        /// This matches HmacOptions.KeyResolver signature.
        /// </summary>
        public static byte[] DeriveHmacKey(byte[] baseKey, int outputBytes)
        {
            if (outputBytes <= 0)
                outputBytes = 32; // default to 32 bytes if someone passes 0

            return DerivePerUserKey(baseKey, outputBytes);
        }

        // ------------------------------------------------------------
        // INTERNAL: Unique KDF
        // ------------------------------------------------------------

        /// <summary>
        /// Simple derivation: HMAC-SHA256(baseKey, userId|deviceId) then truncate.
        /// This is still client-side, so it's obfuscation not true secret storage,
        /// but much better than plain static keys.
        /// </summary>
        private static byte[] DerivePerUserKey(byte[] baseKey, int outputBytes)
        {
            if (baseKey == null || baseKey.Length == 0 || outputBytes <= 0)
                return Array.Empty<byte>();

            // Mix in identity
            //string id = $"{CurrentUserId}|{CurrentDeviceId}";
            var id = UniqueId;
            byte[] idBytes = Encoding.UTF8.GetBytes(id);

            byte[] full;
            using (var hmac = new HMACSHA256(baseKey))
            {
                full = hmac.ComputeHash(idBytes);
            }

            if (full.Length == outputBytes)
                return full;

            if (full.Length > outputBytes)
            {
                var trimmed = new byte[outputBytes];
                Buffer.BlockCopy(full, 0, trimmed, 0, outputBytes);
                return trimmed;
            }

            // If someone asks for >32 bytes, repeat the hash (simple HKDF-ish)
            var result = new byte[outputBytes];
            int offset = 0;
            int remaining = outputBytes;

            int round = 0;
            byte[] prev = Array.Empty<byte>();

            while (remaining > 0)
            {
                using (var h = new HMACSHA256(baseKey))
                {
                    // HMAC(baseKey, prev || id || round)
                    byte[] roundInput = BuildRoundInput(prev, idBytes, round);
                    prev = h.ComputeHash(roundInput);
                }

                int copy = Math.Min(prev.Length, remaining);
                Buffer.BlockCopy(prev, 0, result, offset, copy);
                offset += copy;
                remaining -= copy;
                round++;
            }

            return result;
        }

        private static byte[] BuildRoundInput(byte[] prev, byte[] idBytes, int round)
        {
            byte[] roundBytes = BitConverter.GetBytes(round);
            int len = (prev?.Length ?? 0) + idBytes.Length + roundBytes.Length;

            var buf = new byte[len];
            int pos = 0;

            if (prev != null && prev.Length > 0)
            {
                Buffer.BlockCopy(prev, 0, buf, pos, prev.Length);
                pos += prev.Length;
            }

            Buffer.BlockCopy(idBytes, 0, buf, pos, idBytes.Length);
            pos += idBytes.Length;

            Buffer.BlockCopy(roundBytes, 0, buf, pos, roundBytes.Length);

            return buf;
        }
    }
}
