using Flowsave.Compression;
using Flowsave.Security;
using Flowsave.Security.Options;
using Flowsave.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Flowsave.Namespaces
{
    [CreateAssetMenu(fileName = "FlowSaveConfiguration", menuName = "FlowSave/Config Repository", order = 2)]
    public class FlowSaveConfiguration : ScriptableObject
    {
        [Header("Namespace Assets")]
        [SerializeField] List<NamespaceConfiguration> namespaces = new();

        [Header("Global (fallback) Asset")]
        [field: SerializeField] NamespaceConfiguration DefaultNamespace;
        public SerializationOptions DefaultSerializationOptions;
        public EncryptionOptions DefaultEncryptionOptions;
        public SigningOptions DefaultSigningOptions;
        public CompressionOptions DefaultCompressionOptions;

#if UNITY_EDITOR
        [Header("Editor Only")]
        [SerializeField] bool forceModeInEditor;
        [SerializeField] EnvironmentMode forcedEditorMode = EnvironmentMode.Editor;
#endif

        Dictionary<string, NamespaceConfiguration> _byNamespace;
    }
}
