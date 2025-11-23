#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Flowsave.Namespaces;
using Flowsave.Compression;
using Flowsave.Operations;
using Flowsave.Operations.Options;
using Flowsave.Serialization;
using Flowsave.Storage;
using System;

public partial class FlowSaveConfigWindow
{
    /// <summary>
    /// Tab: global defaults + default environments.
    /// </summary>
    private class GlobalDefaultsTab : IFlowSaveConfigTab
    {
        public string Title => "Defaults & Environments";

        private Vector2 _scroll;

        // The app modes we want to ensure exist as default environments
        private static readonly AppMode[] DefaultAppModes =
        {
            AppMode.Editor,
            AppMode.Development,
            AppMode.Release
        };

        public void Draw(SerializedObject config)
        {
            if (config == null)
                return;

            // ─────────────────────────────────────────────
            // 1. Default options section
            // ─────────────────────────────────────────────
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            // ─────────────────────────────────────────────
            // 2. Default environments section
            // ─────────────────────────────────────────────
            EditorGUILayout.LabelField("Default Environments", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var envsProp = config.FindProperty(nameof(FlowSaveConfiguration.DefaultEnvironments));
            if (envsProp == null)
            {
                EditorGUILayout.HelpBox(
                    "DefaultEnvironments list not found on FlowSaveConfiguration.",
                    MessageType.Error);
                return;
            }

            // Ensure we have environments for each app mode
            EnsureDefaultEnvironments(envsProp);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // Draw environments with:
            // - no Add button
            // - no delete button
            // - AppMode not editable
            FlowSaveEnvironmentDrawer.DrawEnvironmentList(
                config,
                envsProp,
                labelPrefix: "Environment",
                addButtonLabel: null,
                showAddButton: false,
                compactOptions: false,
                allowDelete: false,
                allowAppModeEdit: false);

            EditorGUILayout.EndScrollView();
        }

        private static void EnsureDefaultEnvironments(SerializedProperty envsProp)
        {
            // Build mask of all valid bits for safety
            int allMask = 0;
            foreach (AppMode m in Enum.GetValues(typeof(AppMode)))
            {
                if (m == AppMode.None) continue;
                allMask |= (int)m;
            }

            foreach (var mode in DefaultAppModes)
            {
                int modeMask = (int)mode & allMask;
                if (modeMask == 0)
                    continue;

                bool found = false;

                // Treat any env whose AppMode covers this bit as "existing"
                for (int i = 0; i < envsProp.arraySize; i++)
                {
                    var envProp = envsProp.GetArrayElementAtIndex(i);
                    var appModeProp = envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.AppMode));
                    if (appModeProp == null)
                        continue;

                    int value = appModeProp.intValue & allMask;

                    if ((value & modeMask) == modeMask)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                // Create new environment for this mode
                int index = envsProp.arraySize;
                envsProp.InsertArrayElementAtIndex(index);
                var newEnv = envsProp.GetArrayElementAtIndex(index);

                var appModePropNew = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.AppMode));
                if (appModePropNew != null)
                    appModePropNew.intValue = modeMask;

                var schemaProp = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.SchemaVersion));
                if (schemaProp != null)
                    schemaProp.intValue = 1;

                // Clear operations so we don't inherit from previous element
                var opsProp = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.Operations));
                if (opsProp != null)
                    opsProp.ClearArray();

                // Start with defaults for all option blocks
                FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.StorageOptions), nameof(StorageOptions.UseDefault), true);
                FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.CompressionOptions), nameof(CompressionOptions.UseDefault), true);
                FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.SerializationOptions), nameof(SerializationOptions.UseDefault), true);
                FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.EncryptionOptions), nameof(EncryptionOptions.UseDefault), true);
                FlowSaveEnvironmentDrawer.SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.SigningOptions), nameof(SigningOptions.UseDefault), true);

                newEnv.isExpanded = false;
            }
        }
    }
}

#endif
