namespace Flowsave.Security
{
    public enum NonceStrategy : byte
    {
        Random = 0,
        Counter = 1,
        Deterministic = 2,
    }
}
