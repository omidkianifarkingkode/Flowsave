using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flowsave.Namespaces
{
    [Serializable]
    public class NamespaceConfiguration
    {
        public string NamespaceId = "[namespace]";
        public List<EnvironmentConfiguration> Environments = new();
    }
}
