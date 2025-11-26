using System;
using System.Collections.Generic;

namespace Flowsave.Configurations
{
    [Serializable]
    public class NamespaceConfiguration
    {
        public string NamespaceId = "[namespace]";
        public List<EnvironmentConfiguration> Environments = new();
    }
}
