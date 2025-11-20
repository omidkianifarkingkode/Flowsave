//#if UNITY_EDITOR
//using System;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using Flowsave.Namespaces;
//using Flowsave.Security;
//using Flowsave.Storage;
//using Flowsave.Compression;
//using Flowsave.Serialization;
//using Flowsave.Security.Options;

//[CustomEditor(typeof(NamespaceConfiguration))]
//public class NamespaceConfigurationEditor : Editor
//{
//    private SerializedProperty _namespaceIdProp;
//    private SerializedProperty _envArrayProp;

//    private SerializedProperty _overrideDevProp;
//    private SerializedProperty _overrideReleaseProp;

//    // Backing field names for auto-properties
//    private const string FIELD_ENVIRONMENT = "<Environment>k__BackingField";
//    private const string FIELD_OPERATIONS = "<Operations>k__BackingField";
//    private const string FIELD_STORAGE_OPTIONS = "<StorageOptions>k__BackingField";
//    private const string FIELD_COMPRESSION_OPTIONS = "<CompressionOptions>k__BackingField";
//    private const string FIELD_SERIALIZATION_OPTIONS = "<SerializationOptions>k__BackingField";
//    private const string FIELD_ENCRYPTION_OPTIONS = "<EncryptionOptions>k__BackingField";
//    private const string FIELD_SIGNING_OPTIONS = "<SigningOptions>k__BackingField";
//    private const string FIELD_SCHEMA_VERSION = "<SchemaVersion>k__BackingField";

//    private const string FIELD_NAMESPACE_ID = "<NamespaceId>k__BackingField";

//    private const string FIELD_STORAGE_TYPE = "<StorageType>k__BackingField";
//    private const string FIELD_DISK_STORAGE = "<DiskStorage>k__BackingField";
//    private const string FIELD_PLAYERPREFS_STORAGE = "<PlayerPrefsStorage>k__BackingField";

//    private const string FIELD_COMPRESSION_TYPE = "<CompressionType>k__BackingField";
//    private const string FIELD_COMPRESSION_USE_DEFAULT = "<UseDefault>k__BackingField";

//    private const string FIELD_SERIALIZATION_TYPE = "<SerializationType>k__BackingField";
//    private const string FIELD_SERIALIZATION_USE_DEFAULT = "<UseDefault>k__BackingField";
//    private const string FIELD_JSON_OPTIONS = "<Json>k__BackingField";

//    private const string FIELD_ENCRYPTION_TYPE = "<EncryptionType>k__BackingField";
//    private const string FIELD_ENCRYPTION_USE_DEFAULT = "<UseDefault>k__BackingField";
//    private const string FIELD_AES_128 = "<Aes128>k__BackingField";
//    private const string FIELD_AES_256 = "<Aes256>k__BackingField";

//    private const string FIELD_SIGNING_TYPE = "<SigningType>k__BackingField";
//    private const string FIELD_SIGNING_USE_DEFAULT = "<UseDefault>k__BackingField";
//    private const string FIELD_HMAC = "<Hmac>k__BackingField";

//    private void OnEnable()
//    {
//        _namespaceIdProp = serializedObject.FindProperty(FIELD_NAMESPACE_ID);
//        _envArrayProp = serializedObject.FindProperty("environmentsProperties");

//        _overrideDevProp = serializedObject.FindProperty("overrideDevelopment");
//        _overrideReleaseProp = serializedObject.FindProperty("overrideRelease");

//        EnsureEnvironmentEntries();
//    }

//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update();

//        EditorGUILayout.LabelField("Namespace Configuration", EditorStyles.boldLabel);
//        EditorGUILayout.PropertyField(_namespaceIdProp);

//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("Environments", EditorStyles.boldLabel);

//        // Get the three env entries
//        var editorEnv = GetEnvironmentProperty(EnvironmentMode.Editor);
//        var devEnv = GetEnvironmentProperty(EnvironmentMode.Development);
//        var releaseEnv = GetEnvironmentProperty(EnvironmentMode.Release);

//        if (editorEnv == null || devEnv == null || releaseEnv == null)
//        {
//            EditorGUILayout.HelpBox(
//                "Internal error: environment entries missing. Click 'Fix Environments' to regenerate.",
//                MessageType.Error);

//            if (GUILayout.Button("Fix Environments"))
//            {
//                EnsureEnvironmentEntries(force: true);
//            }

//            serializedObject.ApplyModifiedProperties();
//            return;
//        }

//        // ---- EDITOR ENVIRONMENT ----
//        DrawEnvironmentBlock("Editor", editorEnv, isBaseEnvironment: true);

//        EditorGUILayout.Space();

//        // ---- DEVELOPMENT ENVIRONMENT ----
//        EditorGUILayout.BeginVertical("box");
//        EditorGUILayout.LabelField("Development", EditorStyles.boldLabel);

//        EditorGUILayout.PropertyField(_overrideDevProp, new GUIContent("Override Editor settings"));
//        if (!_overrideDevProp.boolValue)
//        {
//            EditorGUILayout.HelpBox("Using Editor settings (read-only mirror).", MessageType.Info);
//            using (new EditorGUI.DisabledGroupScope(true))
//            {
//                DrawNamespaceProperties(devEnv, showOperationsOnly: true);
//            }

//            if (GUILayout.Button("Sync from Editor now"))
//            {
//                CopyNamespaceProperties(editorEnv, devEnv, keepEnvironmentEnum: true);
//            }
//        }
//        else
//        {
//            DrawNamespaceProperties(devEnv);
//        }

//        EditorGUILayout.EndVertical();

//        EditorGUILayout.Space();

//        // ---- RELEASE ENVIRONMENT ----
//        EditorGUILayout.BeginVertical("box");
//        EditorGUILayout.LabelField("Release", EditorStyles.boldLabel);

//        EditorGUILayout.PropertyField(_overrideReleaseProp, new GUIContent("Override Editor settings"));
//        if (!_overrideReleaseProp.boolValue)
//        {
//            EditorGUILayout.HelpBox("Using Editor settings (read-only mirror).", MessageType.Info);
//            using (new EditorGUI.DisabledGroupScope(true))
//            {
//                DrawNamespaceProperties(releaseEnv, showOperationsOnly: true);
//            }

//            if (GUILayout.Button("Sync from Editor now"))
//            {
//                CopyNamespaceProperties(editorEnv, releaseEnv, keepEnvironmentEnum: true);
//            }
//        }
//        else
//        {
//            DrawNamespaceProperties(releaseEnv);
//        }

//        EditorGUILayout.EndVertical();

//        serializedObject.ApplyModifiedProperties();
//    }

//    #region Environment setup

//    private void EnsureEnvironmentEntries(bool force = false)
//    {
//        serializedObject.Update();

//        var found = new Dictionary<EnvironmentMode, int>();

//        for (int i = 0; i < _envArrayProp.arraySize; i++)
//        {
//            var element = _envArrayProp.GetArrayElementAtIndex(i);
//            var envProp = element.FindPropertyRelative(FIELD_ENVIRONMENT);
//            if (envProp == null) continue;

//            var env = (EnvironmentMode)envProp.enumValueIndex;
//            found[env] = i;
//        }

//        // Create Editor first if missing
//        var editorIndex = EnsureEnvironment(EnvironmentMode.Editor, found);

//        // Then Development and Release, cloning from Editor if possible
//        EnsureEnvironment(EnvironmentMode.Development, found, editorIndex);
//        EnsureEnvironment(EnvironmentMode.Release, found, editorIndex);

//        // Sort: Editor, Development, Release
//        SortEnvironmentArray();

//        serializedObject.ApplyModifiedProperties();
//    }

//    private int EnsureEnvironment(EnvironmentMode env, Dictionary<EnvironmentMode, int> found, int cloneFromIndex = -1)
//    {
//        if (found.TryGetValue(env, out var index))
//            return index;

//        int newIndex = _envArrayProp.arraySize;
//        _envArrayProp.InsertArrayElementAtIndex(newIndex);
//        var newElement = _envArrayProp.GetArrayElementAtIndex(newIndex);

//        // set environment enum
//        var envProp = newElement.FindPropertyRelative(FIELD_ENVIRONMENT);
//        if (envProp != null)
//            envProp.enumValueIndex = (int)env;

//        // clone from Editor if available and we are Dev/Release
//        if (cloneFromIndex >= 0 && env != EnvironmentMode.Editor)
//        {
//            var src = _envArrayProp.GetArrayElementAtIndex(cloneFromIndex);
//            CopyNamespaceProperties(src, newElement, keepEnvironmentEnum: true);
//        }

//        found[env] = newIndex;
//        return newIndex;
//    }

//    private void SortEnvironmentArray()
//    {
//        // desired order: Editor, Development, Release
//        var desiredOrder = new[]
//        {
//            EnvironmentMode.Editor,
//            EnvironmentMode.Development,
//            EnvironmentMode.Release
//        };

//        for (int targetIndex = 0; targetIndex < desiredOrder.Length; targetIndex++)
//        {
//            EnvironmentMode desired = desiredOrder[targetIndex];

//            int currentIndex = -1;
//            for (int i = 0; i < _envArrayProp.arraySize; i++)
//            {
//                var element = _envArrayProp.GetArrayElementAtIndex(i);
//                var envProp = element.FindPropertyRelative(FIELD_ENVIRONMENT);
//                if (envProp == null) continue;

//                if ((EnvironmentMode)envProp.enumValueIndex == desired)
//                {
//                    currentIndex = i;
//                    break;
//                }
//            }

//            if (currentIndex >= 0 && currentIndex != targetIndex)
//            {
//                _envArrayProp.MoveArrayElement(currentIndex, targetIndex);
//            }
//        }
//    }

//    private SerializedProperty GetEnvironmentProperty(EnvironmentMode env)
//    {
//        for (int i = 0; i < _envArrayProp.arraySize; i++)
//        {
//            var element = _envArrayProp.GetArrayElementAtIndex(i);
//            var envProp = element.FindPropertyRelative(FIELD_ENVIRONMENT);
//            if (envProp == null) continue;

//            if ((EnvironmentMode)envProp.enumValueIndex == env)
//                return element;
//        }

//        return null;
//    }

//    /// <summary>
//    /// Copies all children from src to dst, except the Environment enum itself.
//    /// </summary>
//    private void CopyNamespaceProperties(SerializedProperty src, SerializedProperty dst, bool keepEnvironmentEnum)
//    {
//        if (src == null || dst == null) return;

//        // copy operations + options + schemaVersion
//        string srcPrefix = src.propertyPath + ".";
//        string dstPrefix = dst.propertyPath + ".";

//        SerializedProperty srcIter = src.Copy();
//        SerializedProperty srcEnd = src.GetEndProperty();

//        bool enterChildren = true;
//        while (srcIter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(srcIter, srcEnd))
//        {
//            enterChildren = false;

//            string path = srcIter.propertyPath;
//            if (!path.StartsWith(srcPrefix)) break; // outside subtree

//            string relative = path.Substring(srcPrefix.Length);

//            // don't overwrite Environment enum if we want to keep it
//            if (keepEnvironmentEnum && relative == FIELD_ENVIRONMENT)
//                continue;

//            SerializedProperty dstChild = dst.FindPropertyRelative(relative);
//            if (dstChild == null) continue;

//            CopyPropertyValue(srcIter, dstChild);
//        }
//    }

//    private void CopyPropertyValue(SerializedProperty src, SerializedProperty dst)
//    {
//        switch (src.propertyType)
//        {
//            case SerializedPropertyType.Integer:
//                dst.intValue = src.intValue;
//                break;
//            case SerializedPropertyType.Boolean:
//                dst.boolValue = src.boolValue;
//                break;
//            case SerializedPropertyType.Float:
//                dst.floatValue = src.floatValue;
//                break;
//            case SerializedPropertyType.String:
//                dst.stringValue = src.stringValue;
//                break;
//            case SerializedPropertyType.Enum:
//                dst.enumValueIndex = src.enumValueIndex;
//                break;
//            case SerializedPropertyType.ObjectReference:
//                dst.objectReferenceValue = src.objectReferenceValue;
//                break;
//            case SerializedPropertyType.Color:
//                dst.colorValue = src.colorValue;
//                break;
//            case SerializedPropertyType.Vector2:
//                dst.vector2Value = src.vector2Value;
//                break;
//            case SerializedPropertyType.Vector3:
//                dst.vector3Value = src.vector3Value;
//                break;
//            case SerializedPropertyType.Vector4:
//                dst.vector4Value = src.vector4Value;
//                break;
//            case SerializedPropertyType.Rect:
//                dst.rectValue = src.rectValue;
//                break;
//            case SerializedPropertyType.AnimationCurve:
//                dst.animationCurveValue = src.animationCurveValue;
//                break;
//            case SerializedPropertyType.Bounds:
//                dst.boundsValue = src.boundsValue;
//                break;
//            case SerializedPropertyType.Quaternion:
//                dst.quaternionValue = src.quaternionValue;
//                break;
//            default:
//                // arrays, nested classes etc. are handled via their children; no-op here
//                break;
//        }
//    }

//    #endregion

//    #region Drawing NamespaceProperties

//    private void DrawEnvironmentBlock(string label, SerializedProperty envProp, bool isBaseEnvironment = false)
//    {
//        EditorGUILayout.BeginVertical("box");
//        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

//        DrawNamespaceProperties(envProp);

//        EditorGUILayout.EndVertical();
//    }

//    /// <summary>
//    /// Draws a single NamespaceProperties block.
//    /// - Always shows OperationMode
//    /// - Shows per-option groups based on OperationMode flags
//    /// - Inside each group, shows inner content conditionally
//    /// </summary>
//    private void DrawNamespaceProperties(SerializedProperty nsProp, bool showOperationsOnly = false)
//    {
//        if (nsProp == null) return;

//        var operationsProp = nsProp.FindPropertyRelative(FIELD_OPERATIONS);
//        var storageOptionsProp = nsProp.FindPropertyRelative(FIELD_STORAGE_OPTIONS);
//        var compressionOptionsProp = nsProp.FindPropertyRelative(FIELD_COMPRESSION_OPTIONS);
//        var serializationOptionsProp = nsProp.FindPropertyRelative(FIELD_SERIALIZATION_OPTIONS);
//        var encryptionOptionsProp = nsProp.FindPropertyRelative(FIELD_ENCRYPTION_OPTIONS);
//        var signingOptionsProp = nsProp.FindPropertyRelative(FIELD_SIGNING_OPTIONS);
//        var schemaVersionProp = nsProp.FindPropertyRelative(FIELD_SCHEMA_VERSION);

//        // Operation mode
//        EditorGUILayout.PropertyField(operationsProp);
//        var operations = (OperationMode)operationsProp.intValue;

//        if (showOperationsOnly)
//        {
//            return;
//        }

//        EditorGUILayout.Space();

//        // RULE 2: options hidden when OperationMode == None
//        if (operations == OperationMode.None)
//        {
//            EditorGUILayout.HelpBox("No operations selected. Options are hidden.", MessageType.Info);
//            return;
//        }

//        // STORAGE (shown whenever any operation is active – tweak if you prefer a dedicated flag)
//        EditorGUILayout.BeginVertical("box");
//        EditorGUILayout.LabelField("Storage", EditorStyles.boldLabel);
//        DrawStorageOptions(storageOptionsProp);
//        EditorGUILayout.EndVertical();

//        EditorGUILayout.Space();

//        // COMPRESSION - only when Compression flag is set
//        if (operations.HasFlag(OperationMode.Comperssion))
//        {
//            EditorGUILayout.BeginVertical("box");
//            EditorGUILayout.LabelField("Compression", EditorStyles.boldLabel);
//            DrawCompressionOptions(compressionOptionsProp);
//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space();
//        }

//        // SERIALIZATION - show whenever any operation is active
//        EditorGUILayout.BeginVertical("box");
//        EditorGUILayout.LabelField("Serialization", EditorStyles.boldLabel);
//        DrawSerializationOptions(serializationOptionsProp);
//        EditorGUILayout.EndVertical();

//        EditorGUILayout.Space();

//        // ENCRYPTION - only when Encrypt flag is set
//        if (operations.HasFlag(OperationMode.Encrypt))
//        {
//            EditorGUILayout.BeginVertical("box");
//            EditorGUILayout.LabelField("Encryption", EditorStyles.boldLabel);
//            DrawEncryptionOptions(encryptionOptionsProp);
//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space();
//        }

//        // SIGNING - only when Sign flag is set
//        if (operations.HasFlag(OperationMode.Sign))
//        {
//            EditorGUILayout.BeginVertical("box");
//            EditorGUILayout.LabelField("Signing", EditorStyles.boldLabel);
//            DrawSigningOptions(signingOptionsProp);
//            EditorGUILayout.EndVertical();

//            EditorGUILayout.Space();
//        }

//        // Schema version at the end (optional)
//        EditorGUILayout.PropertyField(schemaVersionProp);
//    }

//    #endregion

//    #region Per-option drawing (inner content rules)

//    // 3. StorageOptions inner check:
//    // if StorageType == FileSystem -> show DiskStorageOptions
//    // if StorageType == PlayerPrefs -> show PlayerPrefsStorageOptions
//    private void DrawStorageOptions(SerializedProperty storageProp)
//    {
//        if (storageProp == null) return;

//        var storageTypeProp = storageProp.FindPropertyRelative(FIELD_STORAGE_TYPE);
//        var diskProp = storageProp.FindPropertyRelative(FIELD_DISK_STORAGE);
//        var prefsProp = storageProp.FindPropertyRelative(FIELD_PLAYERPREFS_STORAGE);

//        EditorGUILayout.PropertyField(storageTypeProp);
//        var type = (StorageType)storageTypeProp.enumValueIndex;

//        EditorGUI.indentLevel++;
//        switch (type)
//        {
//            case StorageType.FileSystem:
//                EditorGUILayout.PropertyField(diskProp, new GUIContent("Disk Storage"), true);
//                break;
//            case StorageType.PlayerPrefs:
//                EditorGUILayout.PropertyField(prefsProp, new GUIContent("PlayerPrefs"), true);
//                break;
//            case StorageType.Cloud:
//            case StorageType.Custom:
//                EditorGUILayout.HelpBox($"No specific options defined for {type}. Implement custom handling if needed.",
//                    MessageType.Info);
//                break;
//        }
//        EditorGUI.indentLevel--;
//    }

//    private void DrawCompressionOptions(SerializedProperty compressionProp)
//    {
//        if (compressionProp == null) return;

//        var typeProp = compressionProp.FindPropertyRelative(FIELD_COMPRESSION_TYPE);
//        var useDefaultProp = compressionProp.FindPropertyRelative(FIELD_COMPRESSION_USE_DEFAULT);

//        EditorGUILayout.PropertyField(typeProp, new GUIContent("Compression Type"));
//        EditorGUILayout.PropertyField(useDefaultProp, new GUIContent("Use Default Implementation"));
//    }

//    private void DrawSerializationOptions(SerializedProperty serializationProp)
//    {
//        if (serializationProp == null) return;

//        var typeProp = serializationProp.FindPropertyRelative(FIELD_SERIALIZATION_TYPE);
//        var useDefaultProp = serializationProp.FindPropertyRelative(FIELD_SERIALIZATION_USE_DEFAULT);
//        var jsonProp = serializationProp.FindPropertyRelative(FIELD_JSON_OPTIONS);

//        EditorGUILayout.PropertyField(typeProp, new GUIContent("Serialization Type"));
//        EditorGUILayout.PropertyField(useDefaultProp, new GUIContent("Use Default Serializer"));

//        var type = (SerializationType)typeProp.enumValueIndex;
//        if (type == SerializationType.Json)
//        {
//            EditorGUI.indentLevel++;
//            EditorGUILayout.PropertyField(jsonProp, new GUIContent("JSON Options"), true);
//            EditorGUI.indentLevel--;
//        }
//    }

//    private void DrawEncryptionOptions(SerializedProperty encryptionProp)
//    {
//        if (encryptionProp == null) return;

//        var typeProp = encryptionProp.FindPropertyRelative(FIELD_ENCRYPTION_TYPE);
//        var useDefaultProp = encryptionProp.FindPropertyRelative(FIELD_ENCRYPTION_USE_DEFAULT);
//        var aes128Prop = encryptionProp.FindPropertyRelative(FIELD_AES_128);
//        var aes256Prop = encryptionProp.FindPropertyRelative(FIELD_AES_256);

//        EditorGUILayout.PropertyField(typeProp, new GUIContent("Encryption Type"));
//        EditorGUILayout.PropertyField(useDefaultProp, new GUIContent("Use Default Encryptor"));

//        var type = (EncryptionType)typeProp.enumValueIndex;
//        EditorGUI.indentLevel++;
//        switch (type)
//        {
//            case EncryptionType.Aes128Gcm:
//                EditorGUILayout.PropertyField(aes128Prop, new GUIContent("AES-128 Options"), true);
//                break;
//            case EncryptionType.Aes256Gcm:
//                EditorGUILayout.PropertyField(aes256Prop, new GUIContent("AES-256 Options"), true);
//                break;
//            case EncryptionType.None:
//            default:
//                EditorGUILayout.HelpBox("No encryption configured.", MessageType.Info);
//                break;
//        }
//        EditorGUI.indentLevel--;
//    }

//    private void DrawSigningOptions(SerializedProperty signingProp)
//    {
//        if (signingProp == null) return;

//        var typeProp = signingProp.FindPropertyRelative(FIELD_SIGNING_TYPE);
//        var useDefaultProp = signingProp.FindPropertyRelative(FIELD_SIGNING_USE_DEFAULT);
//        var hmacProp = signingProp.FindPropertyRelative(FIELD_HMAC);

//        EditorGUILayout.PropertyField(typeProp, new GUIContent("Signing Type"));
//        EditorGUILayout.PropertyField(useDefaultProp, new GUIContent("Use Default Signer"));

//        var type = (SigningType)typeProp.enumValueIndex;

//        if (type == SigningType.Hmac)
//        {
//            EditorGUI.indentLevel++;
//            EditorGUILayout.PropertyField(hmacProp, new GUIContent("HMAC Options"), true);
//            EditorGUI.indentLevel--;
//        }
//        else if (type == SigningType.None)
//        {
//            EditorGUILayout.HelpBox("No signing configured.", MessageType.Info);
//        }
//    }

//    #endregion
//}
//#endif
