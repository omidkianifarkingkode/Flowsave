//using System;
//using System.Security.Cryptography;
//using System.Text;

//namespace Flowsave.Security
//{
//    /// <summary>
//    /// RSA-PSS (SHA-256) signer. Works widely in Unity/.NET.
//    /// </summary>
//    public sealed class RsaPssSigner : ISigner, IDisposable
//    {
//        private readonly RSA _rsa; // may hold private or public only
//        private readonly bool _canSign;
//        private readonly string _signerId;


//        public SigAlgId Alg => SigAlgId.RsaPss2048;
//        public string SignerId => _signerId; // key fingerprint (b64url sha256 of SPKI)
//        public bool IsNoOp => false;


//        public RsaPssSigner(RSA rsa)
//        {
//            _rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));
//            try { _canSign = _rsa.ExportParameters(true).D is not null; }
//            catch { _canSign = false; }
//            _signerId = KeyIdUtil.FromRsaPublicKey(_rsa);
//        }


//        public byte[] Sign(ReadOnlySpan<byte> message)
//        {
//            if (!_canSign) throw new InvalidOperationException("This RSA instance does not contain a private key for signing.");
//            byte[] msg = Compat.ToArray(message);
//            return _rsa.SignData(msg, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
//        }


//        public bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, string signerId)
//        {
//            if (!string.IsNullOrEmpty(signerId) && !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(signerId), Encoding.ASCII.GetBytes(_signerId)))
//                return false;
//            byte[] msg = Compat.ToArray(message);
//            byte[] sig = Compat.ToArray(signature);
//            return _rsa.VerifyData(msg, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
//        }


//        public void Dispose()
//        {
//            _rsa?.Dispose();
//        }
//    }
//}
