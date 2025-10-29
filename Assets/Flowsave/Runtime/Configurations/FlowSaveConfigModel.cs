using Flowsave.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flowsave.Configurations
{
    /// <summary>POCO authoring/runtime model holding per-environment entries.</summary>
    [Serializable]
    public class FlowSaveConfigModel : ISerializationCallbackReceiver
    {
        public string namespaceId = "";
        public List<EnvironmentConfig> environments = new List<EnvironmentConfig>();

        // --- ALWAYS ensure the three environments exist ---
        public void EnsureAllModes()
        {
            var needed = new[] { AppMode.Editor, AppMode.Development, AppMode.Release };
            foreach (var m in needed)
                if (!environments.Any(e => e.mode == m))
                    environments.Add(new EnvironmentConfig { mode = m, fields = new FlowSaveConfigFields() });

            // Keep stable order
            environments = environments
                .OrderBy(e => e.mode == AppMode.Editor ? 0 : e.mode == AppMode.Development ? 1 : 2)
                .ToList();
        }

        public void OnBeforeSerialize() => EnsureAllModes();
        public void OnAfterDeserialize() => EnsureAllModes();

        public FlowSaveConfigSnapshot Resolve(AppMode mode)
        {
            EnsureAllModes();
            var env = environments.First(e => e.mode == mode);
            return new FlowSaveConfigSnapshot(namespaceId, env.fields);
        }
    }
}
