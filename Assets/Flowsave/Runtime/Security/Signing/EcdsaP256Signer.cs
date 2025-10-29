//using System;
//using System.Security.Cryptography;
//using System.Text;

//namespace Flowsave.Security
//{

//    /// <summary>
//    /// ECDSA P-256 with SHA-256 signer. Compact signatures; modern choice.
//    /// </summary>
//    public sealed class EcdsaP256Signer : ISigner, IDisposable
//    {
//        private readonly ECDsa _ecdsa; // may hold private or public only
//        private readonly bool _canSign;
//        private readonly string _signerId;


//        public SigAlgId Alg => SigAlgId.EcdsaP256;
//        public string SignerId => _signerId; // key fingerprint (b64url sha256 of SPKI)
//        public bool IsNoOp => false;


//        public EcdsaP256Signer(ECDsa ecdsa)
//        {
//            _ecdsa = ecdsa ?? throw new ArgumentNullException(nameof(ecdsa));
//            try { _ecdsa.ExportParameters(true); _canSign = true; }
//            catch { _canSign = false; }
//            _signerId = KeyIdUtil.FromEcdsaPublicKey(_ecdsa);
//        }


//        public byte[] Sign(ReadOnlySpan<byte> message)
//        {
//            if (!_canSign) throw new InvalidOperationException("This ECDsa instance does not contain a private key for signing.");
//            byte[] msg = Compat.ToArray(message);
//            return _ecdsa.SignData(msg, HashAlgorithmName.SHA256);
//        }


//        public bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, string signerId)
//        {
//            if (!string.IsNullOrEmpty(signerId) && !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(signerId), Encoding.ASCII.GetBytes(_signerId)))
//                return false;
//            byte[] msg = Compat.ToArray(message);
//            byte[] sig = Compat.ToArray(signature);
//            return _ecdsa.VerifyData(msg, sig, HashAlgorithmName.SHA256);
//        }


//        public void Dispose()
//        {
//            _ecdsa?.Dispose();
//        }
//    }
//}
