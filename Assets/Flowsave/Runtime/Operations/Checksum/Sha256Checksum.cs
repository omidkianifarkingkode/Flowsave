using System;
using System.Security.Cryptography;

namespace Flowsave.Checksum
{
    /// <summary>
    /// SHA-256 digest. Not a MAC; use only for non-adversarial corruption checks,
    /// or pair with AEAD/signatures for security.
    /// </summary>
    public sealed class Sha256Checksum : IChecksum
    {
        public ChecksumAlgId Alg => ChecksumAlgId.SHA256;
        public bool IsNoOp => false;


        public byte[] Compute(ReadOnlySpan<byte> message)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(message.ToArray());
        }


        public bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> digest)
        {
            var calc = Compute(message);
            return CryptographicOperations.FixedTimeEquals(calc, digest.ToArray());
        }
    }
}