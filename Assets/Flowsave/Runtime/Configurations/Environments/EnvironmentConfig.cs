using Flowsave.Shared;
using System;

namespace Flowsave.Configurations
{
    [Serializable]
    public class EnvironmentConfig
    {
        public AppMode mode;
        public FlowSaveConfigFields fields = new();
    }
}
