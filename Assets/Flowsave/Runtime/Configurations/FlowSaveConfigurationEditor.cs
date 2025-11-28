#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Flowsave.Configurations
{
    [CreateAssetMenu(fileName = nameof(FlowSaveConfiguration), menuName = "FlowSave/Config Repository", order = 2)]
    public partial class FlowSaveConfiguration
    {
        [ContextMenu("Print")]
        public void Print()
        {
            var enConfig = GetEnvironmentConfiguration("test");

            Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(enConfig, Newtonsoft.Json.Formatting.Indented));
        }

        [ContextMenu("AddNamespace")]
        public void AddNamespace()
        {
            if (DefaultEnvironments == null || DefaultEnvironments.Count == 0)
            {
                Debug.LogWarning("[FlowSaveConfiguration] DefaultEnvironments is empty – cannot create namespace env.");
                return;
            }

            if (Namespaces == null)
                Namespaces = new List<NamespaceConfiguration>();

            var envConfig = EnvironmentConfiguration.Clone(DefaultEnvironments.First());

            var newNs = new NamespaceConfiguration
            {
                NamespaceId = "test",
                Environments = new List<EnvironmentConfiguration> { envConfig }
            };

            Namespaces.Add(newNs);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

    }
}

#endif
