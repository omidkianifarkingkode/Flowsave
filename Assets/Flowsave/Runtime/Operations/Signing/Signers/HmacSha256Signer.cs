using Flowsave.Security.Options;
using System;
using System.Security.Cryptography;

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
        private HmacOptions hmac;

        public SigningType Alg => SigningType.Hmac; // not a public-key alg; keep as None or add a new enum value
        public string SignerId => _signerId;
        public bool IsNoOp => false;


        public HmacSha256Signer(byte[] key, string keyId)
        {
            _key = (byte[])(key ?? throw new ArgumentNullException(nameof(key))).Clone();
            _signerId = keyId ?? string.Empty;
        }

        public HmacSha256Signer(HmacOptions hmac) : this(hmac.Key, hmac.KeyId) { }

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
}
