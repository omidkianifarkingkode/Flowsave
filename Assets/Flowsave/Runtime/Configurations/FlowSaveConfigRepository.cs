// File: Flowsave/Configurations/FlowSaveConfigRepository.cs
using Flowsave.Shared;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flowsave.Configurations
{
    [CreateAssetMenu(fileName = "FlowSaveConfigRepository", menuName = "FlowSave/Config Repository", order = 2)]
    public class FlowSaveConfigRepository : ScriptableObject
    {
        [Header("Namespace Assets")]
        [SerializeField] List<FlowSaveConfigAsset> namespaces = new();

        [Header("Global (fallback) Asset")]
        [SerializeField] FlowSaveConfigAsset globalAsset;

#if UNITY_EDITOR
        [Header("Editor Only")]
        [SerializeField] bool forceModeInEditor;
        [SerializeField] AppMode forcedEditorMode = AppMode.Editor;
#endif

        Dictionary<string, FlowSaveConfigAsset> _byNamespace;

        void OnEnable()
        {
            globalAsset?.Model?.EnsureAllModes();
            RebuildIndex();
        }

        void OnValidate()
        {
            globalAsset?.Model?.EnsureAllModes();
            RebuildIndex();
        }

        public void RebuildIndex()
        {
            _byNamespace = new Dictionary<string, FlowSaveConfigAsset>();
            foreach (var asset in namespaces.Where(a => a != null))
            {
                asset.Model?.EnsureAllModes();
                var key = asset.NamespaceId ?? "";
                if (string.IsNullOrEmpty(key)) continue;
                if (_byNamespace.ContainsKey(key))
                {
                    Debug.LogWarning($"Duplicate FlowSaveConfigAsset for namespace '{key}'. Using first occurrence.");
                    continue;
                }
                _byNamespace[key] = asset;
            }
        }

        public bool TryGet(string ns, out FlowSaveConfigAsset asset)
        {
            if (_byNamespace == null) RebuildIndex();
            return _byNamespace.TryGetValue(ns, out asset);
        }

        public FlowSaveConfigAsset GetGlobalAsset() => globalAsset;

        public AppMode? GetForcedEditorModeOrNull()
        {
#if UNITY_EDITOR
            return forceModeInEditor ? forcedEditorMode : default;
#else
            return null;
#endif
        }

        // Expose list for editor tooling
        public List<FlowSaveConfigAsset> Namespaces => namespaces;
        public void SetNamespaces(List<FlowSaveConfigAsset> list) => namespaces = list;
        public void SetGlobalAsset(FlowSaveConfigAsset asset) => globalAsset = asset;
    }
}
