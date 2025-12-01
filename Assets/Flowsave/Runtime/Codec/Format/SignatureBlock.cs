namespace Flowsave.Codec
{
    public sealed class SignatureBlock
    {
        public string AlgorithmId { get; set; } // "hmac-sha256"
        public string KeyId { get; set; }       // "hmac-main"
        public byte[] Value { get; set; }       // raw signature/MAC bytes
    }
}
