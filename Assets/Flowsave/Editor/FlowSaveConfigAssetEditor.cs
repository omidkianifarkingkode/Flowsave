// File: Flowsave/Configurations/Editor/FlowSaveConfigAssetEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Flowsave.Shared;

namespace Flowsave.Configurations.Editor
{
    [CustomEditor(typeof(FlowSaveConfigAsset))]
    public class FlowSaveConfigAssetEditor : UnityEditor.Editor
    {
        SerializedProperty _model;
        SerializedProperty _namespaceId;
        SerializedProperty _environments;

        static readonly AppMode[] Modes = { AppMode.Editor, AppMode.Development, AppMode.Release };

        void OnEnable()
        {
            _model = serializedObject.FindProperty("model");
            _namespaceId = _model.FindPropertyRelative("namespaceId");
            _environments = _model.FindPropertyRelative("environments");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("FlowSave Config (Asset)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_namespaceId, new GUIContent("Namespace Id"));

            EnsureAllModes();

            EditorGUILayout.Space(6);
            for (int i = 0; i < Modes.Length; i++)
            {
                var envProp = FindEnvProp(Modes[i]);
                var fieldsProp = envProp.FindPropertyRelative("fields");

                var foldoutKey = GetFoldoutKey(Modes[i]);
                bool open = SessionState.GetBool(foldoutKey, true);
                open = EditorGUILayout.Foldout(open, $"{Modes[i]} Environment", true);
                SessionState.SetBool(foldoutKey, open);

                if (open)
                {
                    EditorGUILayout.BeginVertical("box");
                    DrawFields(fieldsProp, Modes[i]);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(4);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        string GetFoldoutKey(AppMode mode)
        {
            var id = target ? target.GetInstanceID() : 0;
            return $"FlowSave_Foldout_{id}_{mode}";
        }

        SerializedProperty FindEnvProp(AppMode mode)
        {
            for (int i = 0; i < _environments.arraySize; i++)
            {
                var el = _environments.GetArrayElementAtIndex(i);
                if ((AppMode)el.FindPropertyRelative("mode").enumValueIndex == mode)
                    return el;
            }
            _environments.arraySize++;
            var created = _environments.GetArrayElementAtIndex(_environments.arraySize - 1);
            created.FindPropertyRelative("mode").enumValueIndex = (int)mode;
            return created;
        }

        void EnsureAllModes()
        {
            foreach (var m in Modes) FindEnvProp(m);
        }

        void DrawFields(SerializedProperty fields, AppMode mode)
        {
            EditorGUILayout.PropertyField(fields.FindPropertyRelative("providerType"));
            EditorGUILayout.PropertyField(fields.FindPropertyRelative("serializerType"));

            // SecurityOptions flags
            var secProp = fields.FindPropertyRelative("securityOptions");
            var current = (SecurityOptions)secProp.intValue;
            var updated = (SecurityOptions)EditorGUILayout.EnumFlagsField(new GUIContent("Security Options"), current);
            if (updated != current) secProp.intValue = (int)updated;

            EditorGUILayout.PropertyField(fields.FindPropertyRelative("encryptionProfileId"));

            // Path root + path + resolved preview
            EditorGUILayout.PropertyField(fields.FindPropertyRelative("pathRoot"), new GUIContent("Path Root"));
            EditorGUILayout.PropertyField(fields.FindPropertyRelative("path"), new GUIContent("Path (relative or absolute)"));

            var root = (StoragePathRoot)fields.FindPropertyRelative("pathRoot").enumValueIndex;
            var sub = fields.FindPropertyRelative("path").stringValue;
            var ns = _namespaceId.stringValue ?? "";
            if (!string.IsNullOrEmpty(sub)) sub = sub.Replace("{NAMESPACE}", ns);
            string resolved = PathResolver.Resolve(root, sub);
            EditorGUILayout.HelpBox($"Resolved path: {resolved}", MessageType.Info);

            EditorGUILayout.PropertyField(fields.FindPropertyRelative("enableBackups"));
            EditorGUILayout.PropertyField(fields.FindPropertyRelative("maxBackups"));
            EditorGUILayout.PropertyField(fields.FindPropertyRelative("schemaVersion"));
        }
    }
}
#endif
