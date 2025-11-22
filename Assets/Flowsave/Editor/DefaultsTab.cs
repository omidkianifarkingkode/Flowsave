#if UNITY_EDITOR

using UnityEditor;
using Flowsave.Namespaces;

public partial class FlowSaveConfigWindow
{
    /// <summary>
    /// Tab: default options.
    /// </summary>
    private class DefaultsTab : IFlowSaveConfigTab
    {
        public string Title => "Defaults Options";

        public void Draw(SerializedObject config)
        {
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(
                config.FindProperty(nameof(FlowSaveConfiguration.DefaultStorageOptions)),
                true);

            EditorGUILayout.PropertyField(
                config.FindProperty(nameof(FlowSaveConfiguration.DefaultCompressionOptions)),
                true);

            EditorGUILayout.PropertyField(
                config.FindProperty(nameof(FlowSaveConfiguration.DefaultSerializationOptions)),
                true);

            EditorGUILayout.PropertyField(
                config.FindProperty(nameof(FlowSaveConfiguration.DefaultEncryptionOptions)),
                true);

            EditorGUILayout.PropertyField(
                config.FindProperty(nameof(FlowSaveConfiguration.DefaultSigningOptions)),
                true);
        }
    }
}

#endif
