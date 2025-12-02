using FlowSave.KeyStorage;
using System;
using System.Security.Cryptography;
using System.Text;

namespace FlowSave.Signing
{
    public sealed class HmacSha256Signer : ISigner
    {
        private readonly byte[] _key;
        private readonly int _truncateBytes;

        public SigningType Alg => SigningType.Hmac;
        public bool IsNoOp => false;
        public string KeyId { get; }

        public HmacSha256Signer(KeyDefinition def)
        {
            var key = KeyRuntime.ResolveHmacKey(def, out var keyId, out var truncateBytes);

            if (key == null || key.Length == 0)
                throw new ArgumentException("HMAC key cannot be null or empty.");

            _key = (byte[])key.Clone();
            KeyId = keyId ?? string.Empty;
            _truncateBytes = truncateBytes;
        }

        // --------------------------------------------------------------------
        // NEW: Detached signature methods
        // --------------------------------------------------------------------
        public Result<byte[]> ComputeSignature(byte[] payload)
        {
            if (payload == null)
                return Result<byte[]>.Failure("Payload is null.");

            try
            {
                var sig = ComputeSignatureCore(payload);
                return Result<byte[]>.Success(sig);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"HMAC compute signature failed: {ex.Message}");
            }
        }

        public Result VerifySignature(byte[] payload, byte[] signature)
        {
            if (payload == null)
                return Result.Failure("Payload is null.");
            if (signature == null)
                return Result.Failure("Signature is null.");

            try
            {
                var expected = ComputeSignatureCore(payload);

                if (expected.Length != signature.Length)
                    return Result.Failure("Signature length mismatch.");

                if (!CryptographicOperations.FixedTimeEquals(expected, signature))
                    return Result.Failure("Signature invalid.");

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"HMAC verify signature failed: {ex.Message}");
            }
        }

        // --------------------------------------------------------------------
        // LEGACY: envelope-style Sign/Verify (unchanged behavior)
        // --------------------------------------------------------------------
        public Result<byte[]> Sign(byte[] payload)
        {
            if (payload == null)
                return Result<byte[]>.Failure("Payload is null.");

            try
            {
                var signature = ComputeSignatureCore(payload);

                byte[] keyIdBytes = Encoding.UTF8.GetBytes(KeyId);
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

                if (signerId != KeyId)
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

                // verify using detached API
                var verifyResult = VerifySignature(payload, sig);
                if (!verifyResult.IsSuccess)
                    return Result<byte[]>.Failure(verifyResult.Error);

                return Result<byte[]>.Success(payload);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"HMAC verify failed: {ex.Message}");
            }
        }

        // --------------------------------------------------------------------
        // Internal raw HMAC computation
        // --------------------------------------------------------------------
        private byte[] ComputeSignatureCore(byte[] payload)
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
