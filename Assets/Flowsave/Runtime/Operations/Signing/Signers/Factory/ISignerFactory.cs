namespace FlowSave.Signing
{
    public interface ISignerFactory
    {
        ISigner CreateSigner(SigningType signAlg);
    }
}
