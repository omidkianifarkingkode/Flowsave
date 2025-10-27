using Flowsave.Configurations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlowSave.Configurations
{
    public interface IConfigRepository
    {
        IEnumerable<FlowSaveConfig> Configs { get; }

        FlowSaveConfig GetConfig(string namespaceId);
        bool HasConfig(string namespaceId);
    }

    [CreateAssetMenu(fileName = "FlowSaveConfigRepository", menuName = "FlowSave/ConfigRepository", order = 2)]
    public class FlowSaveConfigRepository : ScriptableObject, IConfigRepository
    {
        [SerializeField] List<FlowSaveConfig> configs = new();

        public IEnumerable<FlowSaveConfig> Configs => configs.AsReadOnly();

        public FlowSaveConfig GetConfig(string namespaceId)
        {
            var existingConfigs = configs.FirstOrDefault(config => config.namespaceId == namespaceId);

            if (existingConfigs == null)
                return DefaultFlowSaveConfig.GetDefaultConfig(namespaceId);

            return existingConfigs;
        }

        public bool HasConfig(string namespaceId)
        {
            return configs.Exists(config => config.namespaceId == namespaceId);
        }

        public void LoadConfigsFromResources()
        {
            configs = new List<FlowSaveConfig>(Resources.LoadAll<FlowSaveConfig>("Configurations"));
        }
    }
}
