// File: Flowsave/Configurations/FlowSaveConfigAsset.cs
using Flowsave.Shared;
using UnityEngine;

namespace Flowsave.Configurations
{
    /// <summary>Authoring asset that holds a model (Base + per-mode overrides).</summary>
    [CreateAssetMenu(fileName = "FlowSaveConfigAsset", menuName = "FlowSave/Config Asset", order = 1)]
    public class FlowSaveConfigAsset : ScriptableObject
    {
        [SerializeField] FlowSaveConfigModel model = new FlowSaveConfigModel();

        public string NamespaceId => model?.namespaceId ?? "";
        public FlowSaveConfigModel Model => model ?? new FlowSaveConfigModel();

        public FlowSaveConfigSnapshot Resolve(AppMode mode) => (model ?? new FlowSaveConfigModel()).Resolve(mode);
    }
}
