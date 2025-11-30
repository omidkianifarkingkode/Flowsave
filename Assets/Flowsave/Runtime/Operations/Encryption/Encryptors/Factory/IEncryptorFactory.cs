namespace FlowSave.Encryption
{
    public interface IEncryptorFactory
    {
        IEncryptor CreateEncryptor(EncryptionType cryptoAlgId);
    }
}
