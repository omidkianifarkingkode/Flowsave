using Flowsave.Compression;
using Flowsave.Security;
using Flowsave.Security.Options;
using Flowsave.Serialization;
using Flowsave.Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Flowsave.Configurations
{
    [CreateAssetMenu(fileName = "FlowSaveConfigRepository", menuName = "FlowSave/Config Repository", order = 2)]
    public class FlowSaveConfigRepository : ScriptableObject
    {
        [Header("Namespace Assets")]
        [SerializeField] List<NamespaceConfiguration> namespaces = new();

        [Header("Global (fallback) Asset")]
        NamespaceConfiguration DefaultNamespace;
        public SerializationOptions DefaultSerializationOptions;
        public EncryptionOptions DefaultEncryptionOptions;
        public SigningOptions DefaultSigningOptions;
        public CompressionOptions DefaultCompressionOptions;

#if UNITY_EDITOR
        [Header("Editor Only")]
        [SerializeField] bool forceModeInEditor;
        [SerializeField] AppMode forcedEditorMode = AppMode.Editor;
#endif

        Dictionary<string, NamespaceConfiguration> _byNamespace;

        void OnEnable()
        {
            DefaultNamespace.EnsureAllModes();
        }

        void OnValidate()
        {
            DefaultNamespace?.EnsureAllModes();
        }
    }
}
