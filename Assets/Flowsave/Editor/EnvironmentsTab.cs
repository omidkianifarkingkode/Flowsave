#if UNITY_EDITOR

using Flowsave.Compression;
using Flowsave.Namespaces;
using Flowsave.Operations;
using Flowsave.Operations.Options;
using Flowsave.Serialization;
using Flowsave.Storage;
using System;
using UnityEditor;
using UnityEngine;

public partial class FlowSaveConfigWindow
{
    /// <summary>
    /// Tab: global environments.
    /// </summary>
    private class EnvironmentsTab : IFlowSaveConfigTab
    {
        public string Title => "Default Environments";

        private Vector2 _scroll;
        private AppMode _newEnvAppMode = AppMode.Editor;  // default selection

        public void Draw(SerializedObject config)
        {
            EditorGUILayout.LabelField("Default Environments", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var envsProp = config.FindProperty(nameof(FlowSaveConfiguration.DefaultEnvironments));

            if (envsProp == null)
            {
                EditorGUILayout.HelpBox("DefaultEnvironments list not found on FlowSaveConfiguration.", MessageType.Error);
                EditorGUILayout.EndScrollView();   // ensure scroll view ends
                return;
            }

            // Draw list WITHOUT the built-in Add button
            FlowSaveEnvironmentDrawer.DrawEnvironmentList(
                config,
                envsProp,
                labelPrefix: "Environment",
                addButtonLabel: "Add Environment",
                showAddButton: false,
                compactOptions: false);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // --- AppMode + Add Environment row (outside scroll) ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("App Mode", GUILayout.Width(80));
            _newEnvAppMode = (AppMode)EditorGUILayout.EnumPopup(_newEnvAppMode);

            if (GUILayout.Button("Add Environment", GUILayout.Height(24)))
            {
                TryAddEnvironment(envsProp);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void TryAddEnvironment(SerializedProperty envsProp)
        {
            if (_newEnvAppMode == AppMode.None)
            {
                EditorUtility.DisplayDialog(
                    "FlowSave - Environment Error",
                    "Please select a valid AppMode before adding an environment.",
                    "OK");
                return;
            }

            // Build mask of all valid AppMode bits (same idea as GetAppModeLabel)
            int allMask = 0;
            foreach (AppMode m in Enum.GetValues(typeof(AppMode)))
            {
                if (m == AppMode.None) continue;
                allMask |= (int)m;
            }

            int newMask = (int)_newEnvAppMode & allMask;

            // Prevent overlapping modes with existing environments
            for (int i = 0; i < envsProp.arraySize; i++)
            {
                var envProp = envsProp.GetArrayElementAtIndex(i);
                var appModeProp = envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.AppMode));
                if (appModeProp == null)
                    continue;

                int existingMask = appModeProp.intValue & allMask;

                // If any bit overlaps, it's a conflict
                if ((existingMask & newMask) != 0)
                {
                    string existingLabel = GetAppModeLabel(appModeProp);

                    EditorUtility.DisplayDialog(
                        "FlowSave - Environment Error",
                        $"Cannot add environment for AppMode '{_newEnvAppMode}'.\n\n" +
                        $"It overlaps with an existing environment using modes: {existingLabel}.\n\n" +
                        "Each AppMode bit can only belong to a single default environment.",
                        "OK");
                    return;
                }
            }

            // If we get here, it's safe to create the new environment
            int index = envsProp.arraySize;
            envsProp.InsertArrayElementAtIndex(index);
            var newEnv = envsProp.GetArrayElementAtIndex(index);

            var appModeProperty = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.AppMode));
            if (appModeProperty != null)
                appModeProperty.intValue = newMask;

            var schemaProp = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.SchemaVersion));
            if (schemaProp != null)
                schemaProp.intValue = 1;

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
