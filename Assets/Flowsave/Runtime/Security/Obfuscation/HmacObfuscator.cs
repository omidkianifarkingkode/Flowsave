using System;
using System.Security.Cryptography;
using System.Text;

namespace Flowsave.Security
{
    public class HmacObfuscator : IFileNameObfuscator
    {
        public string ObfuscateFilename(string filename)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(filename));
            return Convert.ToBase64String(hash).Replace('/', '_').Replace('+', '-'); // URL-safe base64 encoding
        }
    }
}
