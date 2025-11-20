using System;

namespace Flowsave.Namespaces
{
    [Flags]
    public enum EnvironmentMode
    {
        None = 0,
        Editor = 1 << 0,
        Development = 1 << 2,
        Release = 1 << 3,
    }
}