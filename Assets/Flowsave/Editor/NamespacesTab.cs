#if UNITY_EDITOR

using Flowsave.Compression;
using Flowsave.Namespaces;
using Flowsave.Operations;
using Flowsave.Operations.Options;
using Flowsave.Serialization;
using Flowsave.Storage;
using System;
using System.Collections.Generic;   // <-- for HashSet<List>
using UnityEditor;
using UnityEngine;

public partial class FlowSaveConfigWindow
{
    /// <summary>
    /// Tab: namespace overrides.
    /// </summary>
    private class NamespacesTab : IFlowSaveConfigTab
    {
        public string Title => "Defined Namespaces";

        private Vector2 _scroll;
        private string _newNamespaceName = string.Empty;
        private GUIStyle _placeholderStyle;

        private readonly List<AppMode> _newEnvAppModes = new List<AppMode>();

        public void Draw(SerializedObject config)
        {
            if (_placeholderStyle == null)
            {
                _placeholderStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.gray },
                    fontStyle = FontStyle.Italic
                };
            }

            EditorGUILayout.LabelField("Namespaces", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var nsProp = config.FindProperty(nameof(FlowSaveConfiguration.Namespaces));

            if (nsProp == null)
            {
                EditorGUILayout.HelpBox("Namespaces list not found on FlowSaveConfiguration.", MessageType.Error);
                return;
            }

            while (_newEnvAppModes.Count < nsProp.arraySize)
                _newEnvAppModes.Add(AppMode.Editor); // default

            while (_newEnvAppModes.Count > nsProp.arraySize)
                _newEnvAppModes.RemoveAt(_newEnvAppModes.Count - 1);

            // ─────────────────────────────────────────────────────
            // 1. Duplicate detection for existing namespaces
            // ─────────────────────────────────────────────────────
            var existingNames = new HashSet<string>();
            var duplicatedNames = new List<string>();

            for (int i = 0; i < nsProp.arraySize; i++)
            {
                var itemProp = nsProp.GetArrayElementAtIndex(i);
                var idProp = itemProp.FindPropertyRelative(nameof(NamespaceConfiguration.NamespaceId));
                string name = idProp != null ? idProp.stringValue : null;

                if (string.IsNullOrEmpty(name))
                    continue;

                if (!existingNames.Add(name))
                {
                    if (!duplicatedNames.Contains(name))
                        duplicatedNames.Add(name);
                }
            }

            if (duplicatedNames.Count > 0)
            {
                string list = string.Join(", ", duplicatedNames);
                EditorGUILayout.HelpBox(
                    $"Duplicate namespace Id(s) detected: {list}. Namespace Ids must be unique.",
                    MessageType.Error);
            }

            // ─────────────────────────────────────────────────────
            // 2. Scroll view for namespace list
            // ─────────────────────────────────────────────────────
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < nsProp.arraySize; i++)
            {
                var itemProp = nsProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical("box");

                var idProp = itemProp.FindPropertyRelative(nameof(NamespaceConfiguration.NamespaceId));

                string label = idProp != null && !string.IsNullOrEmpty(idProp.stringValue)
                    ? idProp.stringValue
                    : $"Namespace {i}";

                EditorGUILayout.BeginHorizontal();
                itemProp.isExpanded = EditorGUILayout.Foldout(itemProp.isExpanded, label, true);

                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    nsProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (itemProp.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    if (idProp != null)
                        EditorGUILayout.PropertyField(idProp, new GUIContent("Namespace Id"));

                    EditorGUILayout.Space();

                    var envsProp = itemProp.FindPropertyRelative(nameof(NamespaceConfiguration.Environments));
                    EditorGUILayout.LabelField("Environment Overrides", EditorStyles.boldLabel);

                    // 1) Draw existing overrides WITHOUT internal Add button
                    FlowSaveEnvironmentDrawer.DrawEnvironmentList(
                        config,
                        envsProp,
                        labelPrefix: "Env Override",
                        addButtonLabel: "Add Environment Override",
                        showAddButton: false,
                        compactOptions: true,
                        allowDelete: true,
                        allowAppModeEdit: true);

                    // 2) AppMode + Add Override row (per namespace)
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("App Mode", GUILayout.Width(80));
                    _newEnvAppModes[i] = (AppMode)EditorGUILayout.EnumPopup(_newEnvAppModes[i]);

                    if (GUILayout.Button("Add Environment Override", GUILayout.Height(22)))
                    {
                        TryAddNamespaceEnvironment(envsProp, _newEnvAppModes[i]);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }


                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();

            // ─────────────────────────────────────────────────────
            // 3. Add-namespace controls (outside scroll)
            // ─────────────────────────────────────────────────────
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            // Text field (no label) + placeholder
            _newNamespaceName = EditorGUILayout.TextField(GUIContent.none, _newNamespaceName);

            // Draw placeholder text when empty
            var lastRect = GUILayoutUtility.GetLastRect();
            if (string.IsNullOrEmpty(_newNamespaceName) && Event.current.type == EventType.Repaint)
            {
                EditorGUI.LabelField(lastRect, "{namespace-name}", _placeholderStyle);
            }

            if (GUILayout.Button("Add Namespace", GUILayout.Height(24), GUILayout.Width(130)))
            {
                TryAddNamespace(nsProp, existingNames);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void TryAddNamespace(SerializedProperty nsProp, HashSet<string> existingNames)
        {
            string name = _newNamespaceName?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                EditorUtility.DisplayDialog(
                    "FlowSave - Namespace Error",
                    "Namespace name cannot be empty.\n\nPlease enter a valid name before adding.",
                    "OK");
                return;
            }

            // Check duplicates (including ones already in the asset)
            if (existingNames.Contains(name))
            {
                EditorUtility.DisplayDialog(
                    "FlowSave - Namespace Error",
                    $"Namespace '{name}' already exists.\n\nNamespace Ids must be unique.",
                    "OK");
                return;
            }

            int index = nsProp.arraySize;
            nsProp.InsertArrayElementAtIndex(index);
            var newNs = nsProp.GetArrayElementAtIndex(index);

            ResetNamespaceElement(newNs);

            var idProp = newNs.FindPropertyRelative(nameof(NamespaceConfiguration.NamespaceId));
            if (idProp != null)
                idProp.stringValue = name;

            newNs.isExpanded = true;
            _newNamespaceName = string.Empty;
            existingNames.Add(name);
        }


        private static void ResetNamespaceElement(SerializedProperty nsElement)
        {
            if (nsElement == null)
                return;

            // Clear the namespace id
            var idProp = nsElement.FindPropertyRelative(nameof(NamespaceConfiguration.NamespaceId));
            if (idProp != null)
                idProp.stringValue = string.Empty;

            // Clear any environments copied from the previous element
            var envsProp = nsElement.FindPropertyRelative(nameof(NamespaceConfiguration.Environments));
            if (envsProp != null)
                envsProp.ClearArray();

            // If you later add more fields to NamespaceConfiguration,
            // reset them here as well (bools, enums, etc.).
        }

        private void TryAddNamespaceEnvironment(SerializedProperty envsProp, AppMode mode)
        {
            if (mode == AppMode.None)
            {
                EditorUtility.DisplayDialog(
                    "FlowSave - Namespace Environment Error",
                    "Please select a valid AppMode before adding an environment override.",
                    "OK");
                return;
            }

            // Build mask of all valid bits (same as GetAppModeLabel logic)
            int allMask = 0;
            foreach (AppMode m in Enum.GetValues(typeof(AppMode)))
            {
                if (m == AppMode.None) continue;
                allMask |= (int)m;
            }

            int newMask = (int)mode & allMask;

            // Check against existing env overrides inside THIS namespace
            for (int i = 0; i < envsProp.arraySize; i++)
            {
                var envProp = envsProp.GetArrayElementAtIndex(i);
                var appModeProp = envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.AppMode));
                if (appModeProp == null)
                    continue;

                int existingMask = appModeProp.intValue & allMask;

                if ((existingMask & newMask) != 0)
                {
                    string existingLabel = GetAppModeLabel(appModeProp);

                    EditorUtility.DisplayDialog(
                        "FlowSave - Namespace Environment Error",
                        $"Cannot add environment override for AppMode '{mode}'.\n\n" +
                        $"It overlaps with an existing environment override using modes: {existingLabel}.\n\n" +
                        "Each AppMode bit can only have a single override within a namespace.",
                        "OK");
                    return;
                }
            }

            // No conflict: create the new override
            int index = envsProp.arraySize;
            envsProp.InsertArrayElementAtIndex(index);
            var newEnv = envsProp.GetArrayElementAtIndex(index);

            var opsProp = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.Operations));
            opsProp?.ClearArray();

            var appModePropNew = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.AppMode));
            if (appModePropNew != null)
                appModePropNew.intValue = newMask;

            var schemaProp = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.SchemaVersion));
            if (schemaProp != null)
                schemaProp.intValue = 1;

            // Use defaults initially
            FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.StorageOptions), nameof(StorageOptions.UseDefault), true);
            FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.CompressionOptions), nameof(CompressionOptions.UseDefault), true);
            FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.SerializationOptions), nameof(SerializationOptions.UseDefault), true);
            FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.EncryptionOptions), nameof(EncryptionOptions.UseDefault), true);
            FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.SigningOptions), nameof(SigningOptions.UseDefault), true);

            newEnv.isExpanded = true;
        }

    }
}

#endif
