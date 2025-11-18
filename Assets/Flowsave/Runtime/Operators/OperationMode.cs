using System;

namespace Flowsave.Security
{
    [Flags]
    public enum OperationMode
    {
        None = 0,
        Encrypt = 1 << 0,
        Sign = 1 << 1,
        ObfuscateName = 1 << 2,
        Checksum = 1 << 3,
        Comperssion = 1 << 4,
    }
}