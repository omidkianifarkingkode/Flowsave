using FlowSave.KeyStorage;
using System;
using System.Security.Cryptography;
using System.Text;

namespace FlowSave.Signing
{
    public sealed class HmacSha256Signer : ISigner
    {
        private readonly byte[] _key;
        private readonly string _keyId;
        private readonly int _truncateBytes;

        public SigningType Alg => SigningType.Hmac;
        public bool IsNoOp => false;

        public HmacSha256Signer(byte[] key, string keyId, int truncateBytes = 0)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("HMAC key cannot be null or empty.");

            _key = (byte[])key.Clone();
            _keyId = keyId ?? string.Empty;
            _truncateBytes = truncateBytes;
        }

        public HmacSha256Signer(KeyDefinition def)
            : this(KeyRuntime.ResolveHmacKey(def, out var id, out var trunc), id, trunc) { }

        public HmacSha256Signer(HmacOptions opts)
            : this(opts.Key, opts.KeyId, opts.TruncateTo == HmacTruncate.None ? 0 : (int)opts.TruncateTo) { }

        // --------------------------------------------------------------------
        // SIGN
        // --------------------------------------------------------------------
        public Result<byte[]> Sign(byte[] payload)
        {
            if (payload == null)
                return Result<byte[]>.Failure("Payload is null.");

            try
            {
                var signature = ComputeSignature(payload);

                byte[] keyIdBytes = Encoding.UTF8.GetBytes(_keyId);
                if (keyIdBytes.Length > 255)
                    return Result<byte[]>.Failure("SignerId too long (max 255 bytes).");

                int totalLength =
                    1 +                    // alg byte
                    1 +                    // signer id length
                    keyIdBytes.Length +    // signer id
                    4 +                    // payload length
                    payload.Length +
                    signature.Length;

                var env = new byte[totalLength];
                int offset = 0;

                // Header
                env[offset++] = (byte)Alg;
                env[offset++] = (byte)keyIdBytes.Length;

                Buffer.BlockCopy(keyIdBytes, 0, env, offset, keyIdBytes.Length);
                offset += keyIdBytes.Length;

                // payload length (big endian)
                env[offset++] = (byte)(payload.Length >> 24);
                env[offset++] = (byte)(payload.Length >> 16);
                env[offset++] = (byte)(payload.Length >> 8);
                env[offset++] = (byte)(payload.Length);

                // payload
                Buffer.BlockCopy(payload, 0, env, offset, payload.Length);
                offset += payload.Length;

                // signature
                Buffer.BlockCopy(signature, 0, env, offset, signature.Length);

                return Result<byte[]>.Success(env);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"HMAC sign failed: {ex.Message}");
            }
        }

        // --------------------------------------------------------------------
        // VERIFY
        // --------------------------------------------------------------------
        public Result<byte[]> Verify(byte[] signedEnvelope)
        {
            if (signedEnvelope == null)
                return Result<byte[]>.Failure("Signed envelope is null.");

            try
            {
                int offset = 0;

                // algorithm
                if (signedEnvelope.Length < 2)
                    return Result<byte[]>.Failure("Envelope too small.");

                byte alg = signedEnvelope[offset++];
                if (alg != (byte)SigningType.Hmac)
                    return Result<byte[]>.Failure("Envelope algorithm mismatch.");

                // signerId
                int keyIdLen = signedEnvelope[offset++];
                if (keyIdLen < 0 || signedEnvelope.Length < offset + keyIdLen)
                    return Result<byte[]>.Failure("Invalid signerId length.");

                var signerId = Encoding.UTF8.GetString(signedEnvelope, offset, keyIdLen);
                offset += keyIdLen;

                if (signerId != _keyId)
                    return Result<byte[]>.Failure("SignerId mismatch.");

                // payload length
                if (signedEnvelope.Length < offset + 4)
                    return Result<byte[]>.Failure("Invalid payload length field.");

                int payloadLen =
                    (signedEnvelope[offset++] << 24) |
                    (signedEnvelope[offset++] << 16) |
                    (signedEnvelope[offset++] << 8) |
                     signedEnvelope[offset++];

                if (payloadLen < 0)
                    return Result<byte[]>.Failure("Invalid payload length.");

                if (signedEnvelope.Length < offset + payloadLen)
                    return Result<byte[]>.Failure("Envelope truncated.");

                // extract payload
                byte[] payload = new byte[payloadLen];
                Buffer.BlockCopy(signedEnvelope, offset, payload, 0, payloadLen);
                offset += payloadLen;

                // extract signature
                int sigLen = signedEnvelope.Length - offset;
                if (sigLen <= 0)
                    return Result<byte[]>.Failure("Signature missing.");

                byte[] sig = new byte[sigLen];
                Buffer.BlockCopy(signedEnvelope, offset, sig, 0, sigLen);

                // recompute signature
                var expected = ComputeSignature(payload);

                if (expected.Length != sig.Length)
                    return Result<byte[]>.Failure("Signature length mismatch.");

                if (!CryptographicOperations.FixedTimeEquals(expected, sig))
                    return Result<byte[]>.Failure("Signature invalid.");

                return Result<byte[]>.Success(payload);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"HMAC verify failed: {ex.Message}");
            }
        }

        // --------------------------------------------------------------------
        private byte[] ComputeSignature(byte[] payload)
        {
            using var hmac = new HMACSHA256(_key);
            var full = hmac.ComputeHash(payload);

            if (_truncateBytes <= 0 || _truncateBytes >= full.Length)
                return full;

            var truncated = new byte[_truncateBytes];
            Buffer.BlockCopy(full, 0, truncated, 0, _truncateBytes);
            return truncated;
        }
    }
}
