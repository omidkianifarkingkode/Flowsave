//using System;
//using System.Security.Cryptography;

//namespace Flowsave.Security
//{

//    /// <summary>
//    /// Utility helpers for computing stable key identifiers (fingerprints).
//    /// </summary>
//    public static class KeyIdUtil
//    {
//        // base64url without padding
//        private static string B64Url(ReadOnlySpan<byte> bytes)
//        {
//            string s = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
//            return s;
//        }


//        /// <summary>
//        /// Compute a key id as b64url(SHA-256(SPKI DER)) for RSA/ECDSA public keys.
//        /// Compatible with older Unity/.NET targets.
//        /// </summary>
//        public static string FromRsaPublicKey(RSA rsa)
//        {
//            byte[] spki = rsa.ExportSubjectPublicKeyInfo();
//            byte[] hash = Compat.Sha256(spki);
//            return B64Url(hash);
//        }


//        public static string FromEcdsaPublicKey(ECDsa ecdsa)
//        {
//            byte[] spki = ecdsa.ExportSubjectPublicKeyInfo();
//            byte[] hash = Compat.Sha256(spki);
//            return B64Url(hash);
//        }
//    }
//}
