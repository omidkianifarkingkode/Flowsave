namespace Flowsave.Security
{
    public interface ISignerFactory
    {
        ISigner CreateSigner(SigningType signAlg);
    }
}
