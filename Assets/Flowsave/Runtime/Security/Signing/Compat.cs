//using System;
//using System.Security.Cryptography;

//namespace Flowsave.Security
//{
//    internal static class Compat
//    {
//        public static byte[] ToArray(ReadOnlySpan<byte> span)
//        {
//            var arr = new byte[span.Length];
//            span.CopyTo(arr);
//            return arr;
//        }


//        public static byte[] Sha256(byte[] data)
//        {
//            using var sha = SHA256.Create();
//            return sha.ComputeHash(data);
//        }


//        public static byte[] Sha256(ReadOnlySpan<byte> data)
//        {
//            return Sha256(ToArray(data));
//        }
//    }
//}
