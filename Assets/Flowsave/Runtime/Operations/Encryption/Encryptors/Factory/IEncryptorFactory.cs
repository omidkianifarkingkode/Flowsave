namespace Flowsave.Operations
{
    public interface IEncryptorFactory
    {
        IEncryptor CreateSigner(EncryptionType cryptoAlgId);
    }
}
