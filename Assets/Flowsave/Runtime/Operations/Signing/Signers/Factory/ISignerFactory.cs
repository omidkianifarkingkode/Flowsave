namespace Flowsave.Operations
{
    public interface ISignerFactory
    {
        ISigner CreateSigner(SigningType signAlg);
    }
}
