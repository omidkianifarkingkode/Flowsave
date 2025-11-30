#if UNITY_EDITOR

using FlowSave.Compression;
using FlowSave.Configurations;
using FlowSave.Encryption;
using FlowSave.Operations;
using FlowSave.Serialization;
using FlowSave.Signing;
using FlowSave.Storage;
using System;
using UnityEditor;
using UnityEngine;

namespace FlowSave.Editor
{
    public partial class FlowSaveConfigWindow
    {
        // ─────────────────────────────────────────────────────────────
        //  ENVIRONMENT / OPTIONS DRAWING HELPERS
        // ─────────────────────────────────────────────────────────────

        private static class FlowSaveEnvironmentDrawer
        {
            public static void DrawEnvironmentList(
                SerializedObject rootConfig,
                SerializedProperty envsProp,
                string labelPrefix,
                string addButtonLabel,
                bool showAddButton = true,
                bool compactOptions = false,
                bool allowDelete = true,
                bool allowAppModeEdit = true,
                bool showKeyStore = true)
            {
                if (envsProp == null)
                {
                    EditorGUILayout.HelpBox("Environments list not found.", MessageType.Error);
                    return;
                }

                // Default options (used for initializing overrides)
                var defaultStorageProp = rootConfig.FindProperty(nameof(FlowSaveConfiguration.DefaultStorageOptions));
                var defaultCompressionProp = rootConfig.FindProperty(nameof(FlowSaveConfiguration.DefaultCompressionOptions));
                var defaultSerializationProp = rootConfig.FindProperty(nameof(FlowSaveConfiguration.DefaultSerializationOptions));
                var defaultEncryptionProp = rootConfig.FindProperty(nameof(FlowSaveConfiguration.DefaultEncryptionOptions));
                var defaultSigningProp = rootConfig.FindProperty(nameof(FlowSaveConfiguration.DefaultSigningOptions));

                // ─────────────────────────────────────────────────────
                // 1. Draw existing environments
                // ─────────────────────────────────────────────────────
                for (int i = 0; i < envsProp.arraySize; i++)
                {
                    var envProp = envsProp.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginVertical("box");

                    var appModeProp = envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.AppMode));
                    string modeLabel = GetAppModeLabel(appModeProp);
                    string label = $"{labelPrefix}: {modeLabel}";

                    EditorGUILayout.BeginHorizontal();
                    envProp.isExpanded = EditorGUILayout.Foldout(envProp.isExpanded, label, true);

                    if (allowDelete)
                    {
                        if (GUILayout.Button("X", GUILayout.Width(22)))
                        {
                            envsProp.DeleteArrayElementAtIndex(i);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            break;
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    if (envProp.isExpanded)
                    {
                        // AppMode field behavior depends on allowAppModeEdit
                        if (appModeProp != null)
                        {
                            if (allowAppModeEdit)
                            {
                                EditorGUILayout.PropertyField(appModeProp);
                            }
                            else
                            {
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    EditorGUILayout.PropertyField(appModeProp);
                                }
                            }
                        }

                        if (showKeyStore)
                        {
                            var keyStoreProp = envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.KeyStore));
                            if (keyStoreProp != null)
                            {
                                FlowSaveKeyStoreDrawer.DrawKeyStore(keyStoreProp);
                            }
                        }

                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(
                            envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.SchemaVersion)));

                        // ─────────────────────────────────────────────
                        // Storage – ALWAYS
                        // ─────────────────────────────────────────────
                        DrawOverrideSection(
                            "Storage Options",
                            envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.StorageOptions)),
                            defaultStorageProp,
                            nameof(StorageOptions.UseDefault),
                            DrawStorageOptionsContent,
                            compactOptions
                        );

                        // ─────────────────────────────────────────────
                        // Serialization – ALWAYS
                        // ─────────────────────────────────────────────
                        DrawOverrideSection(
                            "Serialization Options",
                            envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.SerializationOptions)),
                            defaultSerializationProp,
                            nameof(SerializationOptions.UseDefault),
                            DrawSerializationOptionsContent,
                            compactOptions
                        );

                        // ─────────────────────────────────────────────
                        // Operations – Use + Overwrite per operation
                        // ─────────────────────────────────────────────
                        var operationsListProp = envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.Operations));
                        if (operationsListProp == null)
                        {
                            EditorGUILayout.HelpBox("Operations list not found.", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);

                            // Compression
                            DrawOperationSection(
                                "Compression",
                                OperationMode.Compression,
                                operationsListProp,
                                envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.CompressionOptions)),
                                defaultCompressionProp,
                                nameof(CompressionOptions.UseDefault),
                                DrawCompressionOptionsContent,
                                compactOptions
                            );

                            // Encryption
                            DrawOperationSection(
                                "Encryption",
                                OperationMode.Encrypt,
                                operationsListProp,
                                envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.EncryptionOptions)),
                                defaultEncryptionProp,
                                nameof(EncryptionOptions.UseDefault),
                                DrawEncryptionOptionsContent,
                                compactOptions
                            );

                            // Signing
                            DrawOperationSection(
                                "Signing",
                                OperationMode.Sign,
                                operationsListProp,
                                envProp.FindPropertyRelative(nameof(EnvironmentConfiguration.SigningOptions)),
                                defaultSigningProp,
                                nameof(SigningOptions.UseDefault),
                                DrawSigningOptionsContent,
                                compactOptions
                            );
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }

                // ─────────────────────────────────────────────────────
                // 2. Add button (OUTSIDE the loop)
                // ─────────────────────────────────────────────────────
                if (showAddButton)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(addButtonLabel, GUILayout.Height(24)))
                    {
                        int index = envsProp.arraySize;
                        envsProp.InsertArrayElementAtIndex(index);
                        var newEnv = envsProp.GetArrayElementAtIndex(index);

                        var appModeProp = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.AppMode));
                        if (appModeProp != null)
                            appModeProp.enumValueIndex = 0; // None

                        var schemaProp = newEnv.FindPropertyRelative(nameof(EnvironmentConfiguration.SchemaVersion));
                        if (schemaProp != null)
                            schemaProp.intValue = 1;

                        SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.StorageOptions), nameof(StorageOptions.UseDefault), true);
                        SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.CompressionOptions), nameof(CompressionOptions.UseDefault), true);
                        SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.SerializationOptions), nameof(SerializationOptions.UseDefault), true);
                        SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.EncryptionOptions), nameof(EncryptionOptions.UseDefault), true);
                        SetUseDefaultFlag(newEnv, nameof(EnvironmentConfiguration.SigningOptions), nameof(SigningOptions.UseDefault), true);

                        newEnv.isExpanded = true;
                    }
                }
            }

            // ─────────────────────────────────────────────────────────
            // Operation section / helpers (unchanged)
            // ─────────────────────────────────────────────────────────

            private static void DrawOperationSection(
                string opLabel,
                OperationMode mode,
                SerializedProperty operationsListProp,
                SerializedProperty optionsProp,
                SerializedProperty defaultOptionsProp,
                string useDefaultPropertyName,
                Action<SerializedProperty, bool> innerContentDrawer,
                bool compactMode)
            {
                if (operationsListProp == null || optionsProp == null)
                    return;

                bool isUsed = HasOperation(operationsListProp, mode);

                var useDefaultProp = optionsProp.FindPropertyRelative(useDefaultPropertyName);
                bool prevOverrideEnabled = useDefaultProp != null ? !useDefaultProp.boolValue : false;
                bool overrideEnabled = prevOverrideEnabled;

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                bool newIsUsed = EditorGUILayout.ToggleLeft($"Use {opLabel}", isUsed, GUILayout.Width(140f));

                if (newIsUsed)
                {
                    overrideEnabled = EditorGUILayout.ToggleLeft($"Overwrite {opLabel} Options", overrideEnabled);
                }

                EditorGUILayout.EndHorizontal();

                if (newIsUsed != isUsed)
                {
                    if (newIsUsed)
                        AddOperation(operationsListProp, mode);
                    else
                        RemoveOperation(operationsListProp, mode);

                    isUsed = newIsUsed;
                }

                if (!isUsed)
                {
                    if (useDefaultProp != null)
                        useDefaultProp.boolValue = true;

                    EditorGUILayout.EndVertical();
                    return;
                }

                if (!prevOverrideEnabled && overrideEnabled && defaultOptionsProp != null)
                {
                    CopyOptionsFromDefaults(defaultOptionsProp, optionsProp);
                }

                if (useDefaultProp != null)
                    useDefaultProp.boolValue = !overrideEnabled;

                if (overrideEnabled && innerContentDrawer != null)
                {
                    EditorGUI.indentLevel++;
                    innerContentDrawer(optionsProp, compactMode);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            private static bool HasOperation(SerializedProperty operationsProp, OperationMode mode)
            {
                if (operationsProp == null)
                    return false;

                for (int i = 0; i < operationsProp.arraySize; i++)
                {
                    var element = operationsProp.GetArrayElementAtIndex(i);
                    if (element.propertyType == SerializedPropertyType.Enum &&
                        element.enumDisplayNames[element.enumValueIndex] == mode.ToString())
                    {
                        return true;
                    }
                }
                return false;
            }

            private static void AddOperation(SerializedProperty operationsProp, OperationMode mode)
            {
                if (operationsProp == null)
                    return;

                if (HasOperation(operationsProp, mode))
                    return;

                int index = operationsProp.arraySize;
                operationsProp.InsertArrayElementAtIndex(index);
                var newElement = operationsProp.GetArrayElementAtIndex(index);

                for (int i = 0; i < newElement.enumDisplayNames.Length; i++)
                {
                    if (newElement.enumDisplayNames[i] == mode.ToString())
                    {
                        newElement.enumValueIndex = i;
                        break;
                    }
                }
            }

            private static void RemoveOperation(SerializedProperty operationsProp, OperationMode mode)
            {
                if (operationsProp == null)
                    return;

                for (int i = 0; i < operationsProp.arraySize; i++)
                {
                    var element = operationsProp.GetArrayElementAtIndex(i);
                    if (element.propertyType == SerializedPropertyType.Enum &&
                        element.enumDisplayNames[element.enumValueIndex] == mode.ToString())
                    {
                        operationsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }

            public static void SetUseDefaultFlag(SerializedProperty envProp, string optionsName, string useDefaultName, bool value)
            {
                var opt = envProp.FindPropertyRelative(optionsName);
                if (opt == null) return;

                var useDefaultProp = opt.FindPropertyRelative(useDefaultName);
                if (useDefaultProp != null)
                    useDefaultProp.boolValue = value;
            }

            // ─────────────────────────────────────────────────────────
            // Generic override section / copy helpers (unchanged)
            // ─────────────────────────────────────────────────────────

            private static void DrawOverrideSection(
                string label,
                SerializedProperty optionsProp,
                SerializedProperty defaultOptionsProp,
                string useDefaultPropertyName,
                Action<SerializedProperty, bool> innerContentDrawer,
                bool compactMode)
            {
                if (optionsProp == null)
                {
                    EditorGUILayout.HelpBox($"{label} property not found.", MessageType.Warning);
                    return;
                }

                var useDefaultProp = optionsProp.FindPropertyRelative(useDefaultPropertyName);
                if (useDefaultProp == null)
                {
                    EditorGUILayout.HelpBox($"{label}: '{useDefaultPropertyName}' flag not found.", MessageType.Warning);
                    return;
                }

                bool prevOverrideEnabled = !useDefaultProp.boolValue;
                bool overrideEnabled = prevOverrideEnabled;

                EditorGUILayout.BeginVertical("box");
                overrideEnabled = EditorGUILayout.ToggleLeft($"Overwrite {label}", overrideEnabled, EditorStyles.boldLabel);

                if (!prevOverrideEnabled && overrideEnabled && defaultOptionsProp != null)
                {
                    CopyOptionsFromDefaults(defaultOptionsProp, optionsProp);
                }

                useDefaultProp.boolValue = !overrideEnabled;

                if (overrideEnabled && innerContentDrawer != null)
                {
                    EditorGUI.indentLevel++;
                    innerContentDrawer(optionsProp, compactMode);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            private static void CopyOptionsFromDefaults(SerializedProperty defaultProp, SerializedProperty targetProp)
            {
                if (defaultProp == null || targetProp == null)
                    return;

                var src = defaultProp.Copy();
                int srcDepth = src.depth;

                if (!src.NextVisible(true))
                    return;

                do
                {
                    if (src.depth <= srcDepth)
                        break;

                    var dstChild = targetProp.FindPropertyRelative(src.name);
                    if (dstChild == null)
                        continue;

                    CopyValue(src, dstChild);

                } while (src.NextVisible(false));
            }

            private static void CopyValue(SerializedProperty src, SerializedProperty dst)
            {
                switch (src.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        dst.boolValue = src.boolValue;
                        break;
                    case SerializedPropertyType.Integer:
                        dst.intValue = src.intValue;
                        break;
                    case SerializedPropertyType.Float:
                        dst.floatValue = src.floatValue;
                        break;
                    case SerializedPropertyType.Enum:
                        dst.enumValueIndex = src.enumValueIndex;
                        break;
                    case SerializedPropertyType.String:
                        dst.stringValue = src.stringValue;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        dst.objectReferenceValue = src.objectReferenceValue;
                        break;
                    case SerializedPropertyType.Vector2:
                        dst.vector2Value = src.vector2Value;
                        break;
                    case SerializedPropertyType.Vector3:
                        dst.vector3Value = src.vector3Value;
                        break;
                    case SerializedPropertyType.Color:
                        dst.colorValue = src.colorValue;
                        break;
                    case SerializedPropertyType.Rect:
                        dst.rectValue = src.rectValue;
                        break;
                    case SerializedPropertyType.Bounds:
                        dst.boundsValue = src.boundsValue;
                        break;
                    default:
                        if (src.hasVisibleChildren)
                        {
                            var srcChild = src.Copy();
                            int depth = srcChild.depth;

                            if (srcChild.NextVisible(true))
                            {
                                do
                                {
                                    if (srcChild.depth <= depth)
                                        break;

                                    var dstNested = dst.FindPropertyRelative(srcChild.name);
                                    if (dstNested != null)
                                    {
                                        CopyValue(srcChild, dstNested);
                                    }

                                } while (srcChild.NextVisible(false));
                            }
                        }
                        break;
                }
            }

            // ─────────────────────────────────────────────────────────
            // Storage / Compression / Serialization content (unchanged)
            // ─────────────────────────────────────────────────────────

            private static void DrawStorageOptionsContent(SerializedProperty storageProp, bool compactMode)
            {
                if (storageProp == null) return;

                var storageTypeProp = storageProp.FindPropertyRelative(nameof(StorageOptions.StorageType));
                if (storageTypeProp == null)
                {
                    EditorGUILayout.HelpBox("StorageType property not found.", MessageType.Warning);
                    return;
                }

                var obfProp = storageProp.FindPropertyRelative(nameof(StorageOptions.ObfuscateFileName));
                EditorGUILayout.Space(2f);
                obfProp.boolValue = EditorGUILayout.ToggleLeft("Obfuscate File Name", obfProp.boolValue);

                EditorGUILayout.PropertyField(storageTypeProp);

                if (!compactMode)
                {
                    EditorGUILayout.PropertyField(storageProp.FindPropertyRelative(nameof(StorageOptions.DiskStorage)), true);
                    EditorGUILayout.PropertyField(storageProp.FindPropertyRelative(nameof(StorageOptions.PlayerPrefsStorage)), true);
                    return;
                }

                if (storageTypeProp.hasMultipleDifferentValues)
                    return;

                var diskProp = storageProp.FindPropertyRelative(nameof(StorageOptions.DiskStorage));
                var prefsProp = storageProp.FindPropertyRelative(nameof(StorageOptions.PlayerPrefsStorage));

                var selected = (StorageType)storageTypeProp.enumValueIndex;

                EditorGUI.indentLevel++;
                switch (selected)
                {
                    case StorageType.FileSystem:
                        if (diskProp != null)
                            EditorGUILayout.PropertyField(diskProp, true);
                        break;

                    case StorageType.PlayerPrefs:
                        if (prefsProp != null)
                            EditorGUILayout.PropertyField(prefsProp, true);
                        break;
                }
                EditorGUI.indentLevel--;
            }

            private static void DrawCompressionOptionsContent(SerializedProperty compProp, bool compactMode)
            {
                if (compProp == null) return;

                EditorGUILayout.PropertyField(compProp.FindPropertyRelative(nameof(CompressionOptions.CompressionType)));
            }

            private static void DrawSerializationOptionsContent(SerializedProperty serProp, bool compactMode)
            {
                if (serProp == null) return;

                var typeProp = serProp.FindPropertyRelative(nameof(SerializationOptions.SerializationType));
                if (typeProp == null)
                {
                    EditorGUILayout.HelpBox("SerializationType property not found.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.PropertyField(typeProp);

                if (!compactMode)
                {
                    EditorGUILayout.PropertyField(serProp.FindPropertyRelative(nameof(SerializationOptions.Json)), true);
                    return;
                }

                if (typeProp.hasMultipleDifferentValues)
                    return;

                var jsonProp = serProp.FindPropertyRelative(nameof(SerializationOptions.Json));
                var selected = (SerializationType)typeProp.enumValueIndex;

                EditorGUI.indentLevel++;
                switch (selected)
                {
                    case SerializationType.Json:
                        if (jsonProp != null)
                            EditorGUILayout.PropertyField(jsonProp, true);
                        break;
                }
                EditorGUI.indentLevel--;
            }

            // ─────────────────────────────────────────────────────────
            // UPDATED: Encryption options (key ids instead of AesOptions)
            // ─────────────────────────────────────────────────────────

            private static void DrawEncryptionOptionsContent(SerializedProperty encProp, bool compactMode)
            {
                if (encProp == null) return;

                var typeProp = encProp.FindPropertyRelative(nameof(EncryptionOptions.EncryptionType));
                if (typeProp == null)
                {
                    EditorGUILayout.HelpBox("EncryptionType property not found.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.PropertyField(typeProp);

                var aes128KeyIdProp = encProp.FindPropertyRelative(nameof(EncryptionOptions.Aes128KeyId));
                var aes256KeyIdProp = encProp.FindPropertyRelative(nameof(EncryptionOptions.Aes256KeyId));

                if (!compactMode)
                {
                    // Show both key ids explicitly
                    if (aes128KeyIdProp != null)
                        EditorGUILayout.PropertyField(aes128KeyIdProp, new GUIContent("AES-128 Key Id"));

                    if (aes256KeyIdProp != null)
                        EditorGUILayout.PropertyField(aes256KeyIdProp, new GUIContent("AES-256 Key Id"));

                    return;
                }

                if (typeProp.hasMultipleDifferentValues)
                    return;

                var selected = (EncryptionType)typeProp.enumValueIndex;

                EditorGUI.indentLevel++;
                switch (selected)
                {
                    case EncryptionType.Aes128Cbc:
                        if (aes128KeyIdProp != null)
                            EditorGUILayout.PropertyField(aes128KeyIdProp, new GUIContent("Key Id"));
                        break;

                    case EncryptionType.Aes256Cbc:
                        if (aes256KeyIdProp != null)
                            EditorGUILayout.PropertyField(aes256KeyIdProp, new GUIContent("Key Id"));
                        break;

                    default:
                        break;
                }
                EditorGUI.indentLevel--;
            }

            // ─────────────────────────────────────────────────────────
            // UPDATED: Signing options (key id instead of HmacOptions)
            // ─────────────────────────────────────────────────────────

            private static void DrawSigningOptionsContent(SerializedProperty signProp, bool compactMode)
            {
                if (signProp == null) return;

                var typeProp = signProp.FindPropertyRelative(nameof(SigningOptions.SigningType));
                if (typeProp == null)
                {
                    EditorGUILayout.HelpBox("SigningType property not found.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.PropertyField(typeProp);

                var hmacKeyIdProp = signProp.FindPropertyRelative(nameof(SigningOptions.HmacKeyId));

                if (!compactMode)
                {
                    if (hmacKeyIdProp != null)
                        EditorGUILayout.PropertyField(hmacKeyIdProp, new GUIContent("HMAC Key Id"));
                    return;
                }

                if (typeProp.hasMultipleDifferentValues)
                    return;

                var selected = (SigningType)typeProp.enumValueIndex;

                EditorGUI.indentLevel++;
                switch (selected)
                {
                    case SigningType.Hmac:
                        if (hmacKeyIdProp != null)
                            EditorGUILayout.PropertyField(hmacKeyIdProp, new GUIContent("Key Id"));
                        break;

                    default:
                        break;
                }
                EditorGUI.indentLevel--;
            }
        }

        private static string GetAppModeLabel(SerializedProperty appModeProp)
        {
            if (appModeProp == null)
                return "?";

            int raw = appModeProp.intValue;

            int allMask = 0;
            foreach (AppMode m in Enum.GetValues(typeof(AppMode)))
            {
                if (m == AppMode.None) continue;
                allMask |= (int)m;
            }

            raw &= allMask;

            if (raw == 0)
                return "None";

            var parts = new System.Collections.Generic.List<string>();

            foreach (AppMode m in Enum.GetValues(typeof(AppMode)))
            {
                if (m == AppMode.None) continue;

                int bit = (int)m;
                if ((raw & bit) == bit)
                {
                    parts.Add(m.ToString());
                }
            }

            if (parts.Count == 0)
                return "Custom";

            if (raw == allMask)
                return "All";

            return string.Join(" | ", parts);
        }
    }
}

#endif
