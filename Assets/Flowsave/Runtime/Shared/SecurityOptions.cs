using System;

namespace Flowsave.Shared
{
    [Flags]
    public enum SecurityOptions
    {
        None = 0,
        Encrypt = 1 << 0,
        Sign = 1 << 1,
        ObfuscateName = 1 << 2,
    }
}