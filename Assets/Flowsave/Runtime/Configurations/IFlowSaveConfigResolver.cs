using System.Collections.Generic;
using UnityEngine;

namespace Flowsave.Configurations
{
    public interface IFlowSaveConfigResolver
    {
        /// <summary>Returns an effective, resolved model for the namespace & current mode.</summary>
        IFlowSaveConfig Resolve(string namespaceId);
    }

    /// <summary>Resolves effective configs (Base + per-mode overlay) and caches them per mode+namespace.</summary>
    public sealed class FlowSaveConfigResolver : IFlowSaveConfigResolver
    {
        private readonly FlowSaveConfigRepository _repo;
        private readonly IAppEnvironment _env;
        private readonly Dictionary<string, IFlowSaveConfig> _cache = new();

        public FlowSaveConfigResolver(FlowSaveConfigRepository repo, IAppEnvironment env)
        {
            _repo = repo;
            _env = env;
        }

        public IFlowSaveConfig Resolve(string namespaceId)
        {
            var mode = _env.GetCurrentMode(_repo.GetForcedEditorModeOrNull());
            string key = $"{mode}:{namespaceId}";
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            FlowSaveConfigSnapshot snap;
            if (_repo.TryGet(namespaceId, out var asset) && asset != null)
            {
                snap = asset.Resolve(mode);
            }
            else
            {
                var def = _repo.GetDefaultAsset();
                if (def != null)
                {
                    snap = def.Resolve(mode);
                }
                else
                {
                    Debug.LogWarning($"FlowSave: No config for '{namespaceId}' and no default asset set. Using empty defaults.");
                    snap = new FlowSaveConfigSnapshot(namespaceId, new FlowSaveConfigFields());
                }
            }

            _cache[key] = snap;
            return snap;
        }
    }

}

