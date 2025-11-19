namespace Flowsave.Security
{
    public interface IEncryptorFactory
    {
        IEncryptor CreateSigner(EncryptionType cryptoAlgId);
    }
}
