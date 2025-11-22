using System;
using System.Collections.Generic;

namespace Flowsave.Namespaces
{
    [Serializable]
    public class NamespaceConfiguration
    {
        public string NamespaceId = "[namespace]";
        public List<EnvironmentConfiguration> Environments = new();
    }
}
