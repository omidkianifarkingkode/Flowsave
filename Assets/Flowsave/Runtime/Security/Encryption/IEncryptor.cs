using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowsave.Security
{
    public interface IEncryptor
    {
        // Algorithm identity (e.g., AES-256-GCM)
        CryptoAlgId Alg { get; }


        // Sizes for the current algorithm
        int NonceSize { get; } // bytes (e.g., 12 for GCM)
        int TagSize { get; } // bytes (e.g., 16 for GCM)


        // Optional: nonce strategy hint
        NonceStrategy Strategy { get; }


        // Encrypt plaintext with associated data (AAD). Must return freshly generated nonce.
        (byte[] Nonce, byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad);


        // Decrypt and authenticate. MUST throw on authentication failure.
        byte[] Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad);
    }
}
