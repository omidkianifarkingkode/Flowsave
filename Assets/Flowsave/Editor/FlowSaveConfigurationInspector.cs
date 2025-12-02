#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using FlowSave.Configurations;

namespace FlowSave.Editor
{
    /// <summary>
    /// Custom inspector for FlowSaveConfiguration.
    /// Hides raw fields by default and routes editing through FlowSaveConfigWindow.
    /// </summary>
    [CustomEditor(typeof(FlowSaveConfiguration))]
    public class FlowSaveConfigurationInspector : UnityEditor.Editor
    {
        private bool _manualEdit;

        // Optional: persist the Manual Edit toggle per-asset
        private string PrefKey
        {
            get
            {
                var path = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(path))
                    path = target.GetInstanceID().ToString();

                return $"FlowSaveConfigurationInspector.ManualEdit.{path}";
            }
        }

        private void OnEnable()
        {
            _manualEdit = EditorPrefs.GetBool(PrefKey, false);
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(PrefKey, _manualEdit);
        }

        public override void OnInspectorGUI()
        {
            // Header
            EditorGUILayout.LabelField("FlowSave Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Button: open the dedicated editor window
            if (GUILayout.Button("Open FlowSave Configuration Window", GUILayout.Height(24)))
            {
                FlowSaveConfigWindow.Open();
            }

            EditorGUILayout.Space();

            // Manual edit toggle
            _manualEdit = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Manual Edit",
                    "Enable this to directly edit the ScriptableObject fields.\n" +
                    "Normally you should use the FlowSave Configuration window."
                ),
                _manualEdit
            );

            if (_manualEdit)
            {
                EditorGUILayout.HelpBox(
                    "Manual editing bypasses FlowSave's validation and UX helpers. " +
                    "Only use this if you know what you're doing.",
                    MessageType.Warning
                );

                EditorGUILayout.Space();

                // Draw the real fields (excluding the script reference)
                serializedObject.Update();
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}

#endif
