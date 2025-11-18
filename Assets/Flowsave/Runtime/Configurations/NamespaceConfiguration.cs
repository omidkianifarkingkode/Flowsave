using Flowsave.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flowsave.Configurations
{
    /// <summary>POCO authoring/runtime model holding per-environment entries.</summary>
    public class NamespaceConfiguration : ScriptableObject, ISerializationCallbackReceiver
    {
        public string namespaceId = "";
        public List<EnvironementFields> environments = new();

        // --- ALWAYS ensure the three environments exist ---
        public void EnsureAllModes()
        {
            
        }

        public void OnBeforeSerialize() => EnsureAllModes();
        public void OnAfterDeserialize() => EnsureAllModes();
    }
}
