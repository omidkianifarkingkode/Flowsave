using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Flowsave.Security
{
    public static class SecurityHelper
    {
        // AES Encryption Helper
        public static byte[] Encrypt(byte[] data, string key, string iv)
        {
            using var aesAlg = Aes.Create();

            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);
            using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using var ms = new MemoryStream();
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }
            return ms.ToArray();
        }

        // AES Decryption Helper
        public static byte[] Decrypt(byte[] data, string key, string iv)
        {
            using var aesAlg = Aes.Create();
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);
            using var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new MemoryStream();
            cs.CopyTo(sr);
            return sr.ToArray();
        }

        // HMAC-SHA256 for signing
        public static byte[] ComputeHmacSha256(byte[] data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return hmac.ComputeHash(data);
        }

        // Obfuscate filename using SHA256
        public static string ObfuscateFilename(string filename)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(filename));
            return Convert.ToBase64String(hash).Replace('/', '_').Replace('+', '-'); // URL-safe base64 encoding
        }
    }
}
