namespace Flowsave.Operations
{
    public interface IEncryptorFactory
    {
        IEncryptor CreateEncryptor(EncryptionType cryptoAlgId);
    }
}
