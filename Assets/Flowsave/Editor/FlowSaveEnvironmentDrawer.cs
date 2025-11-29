#if UNITY_EDITOR

using Flowsave.Compression;
using Flowsave.Configurations;
using Flowsave.Operations;
using Flowsave.Operations.Options;
using Flowsave.Serialization;
using Flowsave.Storage;
using System;
using UnityEditor;
using UnityEngine;

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
            bool allowAppModeEdit = true)
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

            // Is this operation currently used (present in list)?
            bool isUsed = HasOperation(operationsListProp, mode);

            var useDefaultProp = optionsProp.FindPropertyRelative(useDefaultPropertyName);
            bool prevOverrideEnabled = useDefaultProp != null ? !useDefaultProp.boolValue : false;
            bool overrideEnabled = prevOverrideEnabled;

            EditorGUILayout.BeginVertical("box");

            // ─────────────────────────────────────
            // Header row: "Use X" + optional "Overwrite"
            // ─────────────────────────────────────
            EditorGUILayout.BeginHorizontal();

            // "Use" checkbox: controls whether this op is added to the list
            bool newIsUsed = EditorGUILayout.ToggleLeft($"Use {opLabel}", isUsed, GUILayout.Width(140f));

            // Only show "Overwrite" checkbox if operation is enabled
            if (newIsUsed)
            {
                overrideEnabled = EditorGUILayout.ToggleLeft($"Overwrite {opLabel} Options", overrideEnabled);
            }

            EditorGUILayout.EndHorizontal();

            // ─────────────────────────────────────
            // Update operations list if "Use" changed
            // ─────────────────────────────────────
            if (newIsUsed != isUsed)
            {
                if (newIsUsed)
                    AddOperation(operationsListProp, mode);
                else
                    RemoveOperation(operationsListProp, mode);

                isUsed = newIsUsed;
            }

            // If operation is not used, force options to "use default" and bail out
            if (!isUsed)
            {
                if (useDefaultProp != null)
                    useDefaultProp.boolValue = true; // always fall back to defaults when disabled

                EditorGUILayout.EndVertical();
                return;
            }

            // ─────────────────────────────────────
            // Operation is used: manage override state
            // ─────────────────────────────────────

            // When first enabling "override", copy defaults into the local options
            if (!prevOverrideEnabled && overrideEnabled && defaultOptionsProp != null)
            {
                CopyOptionsFromDefaults(defaultOptionsProp, optionsProp);
            }

            if (useDefaultProp != null)
                useDefaultProp.boolValue = !overrideEnabled;

            // Only show options UI if operation is used AND overridden
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

            // avoid duplicates
            if (HasOperation(operationsProp, mode))
                return;

            int index = operationsProp.arraySize;
            operationsProp.InsertArrayElementAtIndex(index);
            var newElement = operationsProp.GetArrayElementAtIndex(index);

            // set by name
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

            // Transition from "using default" -> "overriding": copy from default options
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

            // Always show the type
            EditorGUILayout.PropertyField(storageTypeProp);

            // In default/environments: show everything exactly as before
            if (!compactMode)
            {
                EditorGUILayout.PropertyField(storageProp.FindPropertyRelative(nameof(StorageOptions.DiskStorage)), true);
                EditorGUILayout.PropertyField(storageProp.FindPropertyRelative(nameof(StorageOptions.PlayerPrefsStorage)), true);
                return;
            }

            // Compact behavior (namespace overrides only)
            if (storageTypeProp.hasMultipleDifferentValues)
                return;

            var diskProp = storageProp.FindPropertyRelative(nameof(StorageOptions.DiskStorage));
            var prefsProp = storageProp.FindPropertyRelative(nameof(StorageOptions.PlayerPrefsStorage));

            var selected = (StorageType)storageTypeProp.enumValueIndex;

            EditorGUI.indentLevel++;
            switch (selected)
            {
                case StorageType.FileSystem:     // adjust enum names if different
                    if (diskProp != null)
                        EditorGUILayout.PropertyField(diskProp, true);
                    break;

                case StorageType.PlayerPrefs:
                    if (prefsProp != null)
                        EditorGUILayout.PropertyField(prefsProp, true);
                    break;

                default:
                    // For other storage types you may add extra blocks later
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

            // In default/environments: old behavior (always show Json block)
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

                default:
                    // Other formats (e.g. Binary, MessagePack) use no extra block here
                    break;
            }
            EditorGUI.indentLevel--;
        }

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

            if (!compactMode)
            {
                EditorGUILayout.PropertyField(encProp.FindPropertyRelative(nameof(EncryptionOptions.Aes128)), true);
                EditorGUILayout.PropertyField(encProp.FindPropertyRelative(nameof(EncryptionOptions.Aes256)), true);
                return;
            }

            if (typeProp.hasMultipleDifferentValues)
                return;

            var aes128Prop = encProp.FindPropertyRelative(nameof(EncryptionOptions.Aes128));
            var aes256Prop = encProp.FindPropertyRelative(nameof(EncryptionOptions.Aes256));

            var selected = (EncryptionType)typeProp.enumValueIndex;

            EditorGUI.indentLevel++;
            switch (selected)
            {
                case EncryptionType.Aes128Cbc:
                    if (aes128Prop != null)
                        EditorGUILayout.PropertyField(aes128Prop, true);
                    break;

                case EncryptionType.Aes256Cbc:
                    if (aes256Prop != null)
                        EditorGUILayout.PropertyField(aes256Prop, true);
                    break;

                default:
                    break;
            }
            EditorGUI.indentLevel--;
        }

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

            if (!compactMode)
            {
                EditorGUILayout.PropertyField(signProp.FindPropertyRelative(nameof(SigningOptions.Hmac)), true);
                return;
            }

            if (typeProp.hasMultipleDifferentValues)
                return;

            var hmacProp = signProp.FindPropertyRelative(nameof(SigningOptions.Hmac));
            var selected = (SigningType)typeProp.enumValueIndex;

            EditorGUI.indentLevel++;
            switch (selected)
            {
                case SigningType.Hmac:
                    if (hmacProp != null)
                        EditorGUILayout.PropertyField(hmacProp, true);
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

        // Build mask of all known flags (except None)
        int allMask = 0;
        foreach (AppMode m in Enum.GetValues(typeof(AppMode)))
        {
            if (m == AppMode.None) continue;
            allMask |= (int)m;
        }

        // Strip out any unknown bits (like Unity's "Everything" junk)
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

        // Optional: special-case "all flags on"
        if (raw == allMask)
            return "All";

        return string.Join(" | ", parts);
    }

}

#endif
