using System;
using System.Security.Cryptography;
using UnityEngine;

namespace Flowsave.Security
{
    /// <summary>
    /// HMAC-SHA256 signer (symmetric). Use ONLY when both producer and verifier share the same secret.
    /// Not suitable for proving origin to an untrusted client because any holder of the secret can forge.
    /// </summary>
    public sealed class HmacSha256Signer : ISigner
    {
        private readonly byte[] _key;
        private readonly string _signerId; // key id for rotation/routing


        public SigAlgId Alg => SigAlgId.Hmac; // not a public-key alg; keep as None or add a new enum value
        public string SignerId => _signerId;
        public bool IsNoOp => false;


        public HmacSha256Signer(byte[] key, string keyId)
        {
            _key = (byte[])(key ?? throw new ArgumentNullException(nameof(key))).Clone();
            _signerId = keyId ?? string.Empty;
        }


        public byte[] Sign(ReadOnlySpan<byte> message)
        {
            using var hmac = new HMACSHA256(_key);
            return hmac.ComputeHash(message.ToArray());
        }


        public bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, string signerId)
        {
            if (!string.IsNullOrEmpty(signerId) && signerId != _signerId) return false;
            using var hmac = new HMACSHA256(_key);
            var calc = hmac.ComputeHash(message.ToArray());
            return CryptographicOperations.FixedTimeEquals(calc, signature.ToArray());
        }
    }

    [Serializable]
    public class HmacOptions
    {
        [Tooltip("Base64 of HMAC key bytes (e.g., 32 bytes recommended). TEST ONLY – do not ship real keys.")]
        public string keyB64 = string.Empty;


        [Tooltip("Identifier for this HMAC key (used in envelopes/headers for rotation). Not secret.")]
        public string keyId = "hmac-test";


        [Tooltip("Truncate HMAC output to N bytes (leave 0 for full 32). 16..32 typical. Lower = smaller but weaker.")]
        [Range(0, 32)] public int truncateTo = 0;


        public byte[] Key => string.IsNullOrEmpty(keyB64) ? Array.Empty<byte>() : Convert.FromBase64String(keyB64);


        public void Validate()
        {
            if (truncateTo != 0 && (truncateTo < 10 || truncateTo > 32))
                Debug.LogWarning("[HmacOptions] Truncation outside 10..32 bytes is unusual. Use 16..32 typically, or 0 for full.");
        }
    }
}
