using System;

namespace Flowsave.Checksum
{
    /// <summary>
    /// CRC32C (Castagnoli) checksum. Fast 4-byte checksum for corruption detection.
    /// </summary>
    public sealed class Crc32CChecksum : IChecksum
    {
        public ChecksumAlgId Alg => ChecksumAlgId.CRC32C;
        public bool IsNoOp => false;


        // Precomputed table for Castagnoli polynomial 0x1EDC6F41
        private static readonly uint[] Table = CreateTable();


        public byte[] Compute(ReadOnlySpan<byte> message)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < message.Length; i++)
            {
                byte b = message[i];
                uint idx = (crc ^ b) & 0xFFu;
                crc = Table[idx] ^ (crc >> 8);
            }
            crc ^= 0xFFFFFFFFu;
            // Return big-endian 4 bytes
            return new byte[]
            {
(byte)(crc >> 24),
(byte)(crc >> 16),
(byte)(crc >> 8),
(byte)(crc)
            };
        }


        public bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> digest)
        {
            var calc = Compute(message);
            return digest.Length == 4 && calc[0] == digest[0] && calc[1] == digest[1] && calc[2] == digest[2] && calc[3] == digest[3];
        }


        private static uint[] CreateTable()
        {
            const uint poly = 0x1EDC6F41u;
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ poly;
                    else
                        crc >>= 1;
                }
                table[i] = crc;
            }
            return table;
        }
    }
}