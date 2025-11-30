using System;
using System.Collections.Generic;

namespace FlowSave.Configurations
{
    [Serializable]
    public class NamespaceConfiguration
    {
        public string NamespaceId = "[namespace]";
        public List<EnvironmentConfiguration> Environments = new();
    }
}
