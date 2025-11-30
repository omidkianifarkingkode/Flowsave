#if UNITY_EDITOR

using FlowSave.Configurations;
using FlowSave.Encryption;
using FlowSave.KeyStorage;
using FlowSave.Logging;
using FlowSave.Signing;
using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace FlowSave.Editor
{
    public partial class FlowSaveConfigWindow
    {
        /// <summary>
        /// Helper drawer for EnvironmentConfiguration.KeyStore (KeyStoreOptions).
        /// </summary>
        private static class FlowSaveKeyStoreDrawer
        {
            public static void DrawKeyStore(SerializedProperty keyStoreProp)
            {
                if (keyStoreProp == null)
                    return;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Key Store", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                var keysProp = keyStoreProp.FindPropertyRelative(nameof(KeyStoreOptions.Keys));
                if (keysProp == null)
                {
                    EditorGUILayout.HelpBox("Key list not found on KeyStoreOptions.", MessageType.Warning);
                }
                else
                {
                    DrawKeysList(keysProp);
                }

                EditorGUI.indentLevel--;
            }

            private static void DrawKeysList(SerializedProperty keysProp)
            {
                // Draw existing keys
                for (int i = 0; i < keysProp.arraySize; i++)
                {
                    var keyProp = keysProp.GetArrayElementAtIndex(i);
                    if (keyProp == null)
                        continue;

                    EditorGUILayout.BeginVertical("box");

                    var keyIdProp = keyProp.FindPropertyRelative(nameof(KeyDefinition.KeyId));
                    var kindProp = keyProp.FindPropertyRelative(nameof(KeyDefinition.Kind));
                    var keyBitsProp = keyProp.FindPropertyRelative(nameof(KeyDefinition.KeyBits));
                    var truncProp = keyProp.FindPropertyRelative(nameof(KeyDefinition.TruncateTo));
                    var deriveKeyProp = keyProp.FindPropertyRelative(nameof(KeyDefinition.DeriveKey));
                    var keyB64Prop = keyProp.FindPropertyRelative(nameof(KeyDefinition.KeyB64));

                    string title = keyIdProp != null && !string.IsNullOrEmpty(keyIdProp.stringValue)
                        ? keyIdProp.stringValue
                        : $"Key {i}";

                    // Header row: foldout + delete
                    EditorGUILayout.BeginHorizontal();
                    keyProp.isExpanded = EditorGUILayout.Foldout(keyProp.isExpanded, title, true);

                    if (GUILayout.Button("X", GUILayout.Width(22)))
                    {
                        keysProp.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (keyProp.isExpanded)
                    {
                        EditorGUI.indentLevel++;

                        // Kind
                        KeyKind? kind = null;
                        if (kindProp != null && kindProp.propertyType == SerializedPropertyType.Enum)
                        {
                            EditorGUILayout.PropertyField(kindProp, new GUIContent("Kind"));
                            kind = (KeyKind)kindProp.enumValueIndex;
                        }

                        // KeyId row with "Default" button
                        if (keyIdProp != null)
                            DrawKeyIdRow(keyIdProp, kind);

                        // Kind-specific fields
                        if (kind.HasValue)
                        {
                            switch (kind.Value)
                            {
                                case KeyKind.Aes:
                                    DrawAesKeyFields(keyBitsProp, deriveKeyProp, keyB64Prop);
                                    break;

                                case KeyKind.Hmac:
                                    DrawHmacKeyFields(truncProp, deriveKeyProp, keyB64Prop);
                                    break;
                            }
                        }

                        // Auto-fill KeyId if still empty
                        if (keyIdProp != null && string.IsNullOrEmpty(keyIdProp.stringValue) && kind.HasValue)
                        {
                            string baseId = kind.Value == KeyKind.Aes
                                ? KeyStoreOptions.DefaultAesKeyId
                                : KeyStoreOptions.DefaultHmacKeyId;

                            keyIdProp.stringValue = keysProp.arraySize > 1
                                ? $"{baseId}-{keysProp.arraySize - 1}"
                                : baseId;
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2f);
                }

                // Add key button
                EditorGUILayout.Space();
                if (GUILayout.Button("Add Key", GUILayout.Height(20)))
                {
                    int index = keysProp.arraySize;
                    keysProp.InsertArrayElementAtIndex(index);
                    var newKeyProp = keysProp.GetArrayElementAtIndex(index);

                    var keyIdProp = newKeyProp.FindPropertyRelative(nameof(KeyDefinition.KeyId));
                    var kindProp = newKeyProp.FindPropertyRelative(nameof(KeyDefinition.Kind));
                    var keyBitsProp = newKeyProp.FindPropertyRelative(nameof(KeyDefinition.KeyBits));
                    var truncProp = newKeyProp.FindPropertyRelative(nameof(KeyDefinition.TruncateTo));
                    var deriveKeyProp = newKeyProp.FindPropertyRelative(nameof(KeyDefinition.DeriveKey));
                    var keyB64Prop = newKeyProp.FindPropertyRelative(nameof(KeyDefinition.KeyB64));

                    // Default Kind = AES
                    if (kindProp != null && kindProp.propertyType == SerializedPropertyType.Enum)
                        SetEnumByName(kindProp, nameof(KeyKind.Aes));

                    // Default AES size = 128
                    if (keyBitsProp != null && keyBitsProp.propertyType == SerializedPropertyType.Enum)
                        SetEnumByName(keyBitsProp, nameof(KeyBits._128));

                    // Default HMAC truncate = None
                    if (truncProp != null && truncProp.propertyType == SerializedPropertyType.Enum)
                        SetEnumByName(truncProp, nameof(HmacTruncate.None));

                    if (deriveKeyProp != null)
                        deriveKeyProp.boolValue = false;

                    if (keyB64Prop != null)
                        keyB64Prop.stringValue = string.Empty;

                    if (keyIdProp != null)
                        keyIdProp.stringValue = KeyStoreOptions.DefaultAesKeyId; // default for AES

                    newKeyProp.isExpanded = true;
                }
            }

            // --------------------------------------------------------
            // Key Id row: [Key Id][Default]
            // --------------------------------------------------------

            private static void DrawKeyIdRow(SerializedProperty keyIdProp, KeyKind? kind)
            {
                float line = EditorGUIUtility.singleLineHeight;
                float buttonWidth = 70f;
                float spacing = 4f;

                var rect = GUILayoutUtility.GetRect(0f, line);

                // Label
                var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, line);
                EditorGUI.LabelField(labelRect, "Key Id");

                float remaining = rect.width - EditorGUIUtility.labelWidth;
                float fieldWidth = remaining - (buttonWidth + spacing);

                float x = rect.x + EditorGUIUtility.labelWidth;

                // Text field
                var fieldRect = new Rect(x, rect.y, fieldWidth, line);
                keyIdProp.stringValue = EditorGUI.TextField(fieldRect, GUIContent.none, keyIdProp.stringValue);

                x += fieldWidth + spacing;

                // "Default" button
                var btnRect = new Rect(x, rect.y, buttonWidth, line);
                if (GUI.Button(btnRect, "Default"))
                {
                    string defId = string.Empty;

                    switch (kind)
                    {
                        case KeyKind.Aes:
                            defId = KeyStoreOptions.DefaultAesKeyId;
                            break;
                        case KeyKind.Hmac:
                            defId = KeyStoreOptions.DefaultHmacKeyId;
                            break;
                    }

                    if (!string.IsNullOrEmpty(defId))
                    {
                        keyIdProp.stringValue = defId;
                    }
                }
            }

            // ─────────────────────────────────────────────────────────
            // AES key UI
            // ─────────────────────────────────────────────────────────

            private static void DrawAesKeyFields(
                SerializedProperty keyBitsProp,
                SerializedProperty deriveKeyProp,
                SerializedProperty keyB64Prop)
            {
                if (keyBitsProp != null)
                    EditorGUILayout.PropertyField(keyBitsProp, new GUIContent("Key Size"));

                DrawDeriveRow(deriveKeyProp, keyB64Prop, isAes: true, keyBitsProp: keyBitsProp);
                DrawKeyB64Row(keyB64Prop, isAes: true, keyBitsProp: keyBitsProp);
            }

            // ─────────────────────────────────────────────────────────
            // HMAC key UI
            // ─────────────────────────────────────────────────────────

            private static void DrawHmacKeyFields(
                SerializedProperty truncProp,
                SerializedProperty deriveKeyProp,
                SerializedProperty keyB64Prop)
            {
                if (truncProp != null)
                    EditorGUILayout.PropertyField(truncProp, new GUIContent("Truncate To"));

                DrawDeriveRow(deriveKeyProp, keyB64Prop, isAes: false, keyBitsProp: null);
                DrawKeyB64Row(keyB64Prop, isAes: false, keyBitsProp: null);
            }

            // ─────────────────────────────────────────────────────────
            // Derive row: [Derive Key] [preview] [Copy]
            // ─────────────────────────────────────────────────────────

            private static void DrawDeriveRow(
                SerializedProperty deriveKeyProp,
                SerializedProperty keyB64Prop,
                bool isAes,
                SerializedProperty keyBitsProp)
            {
                if (deriveKeyProp == null)
                    return;

                float line = EditorGUIUtility.singleLineHeight;
                float button = 70f;
                float spacing = 4f;

                var rect = GUILayoutUtility.GetRect(0f, line);

                // Left: checkbox + label
                float deriveWidth = rect.width * 0.4f;
                var deriveRect = new Rect(rect.x, rect.y, deriveWidth, line);
                deriveKeyProp.boolValue = EditorGUI.ToggleLeft(deriveRect, "Derive Key", deriveKeyProp.boolValue);

                if (!deriveKeyProp.boolValue)
                    return;

                // Compute derived key for preview
                string derivedB64 = isAes
                    ? ComputeDerivedAesBase64(keyB64Prop, keyBitsProp)
                    : ComputeDerivedHmacBase64(keyB64Prop);

                // Middle: readonly text (truncated)
                float labelWidth = rect.width - deriveWidth - button - spacing * 2f;
                var labelRect = new Rect(rect.x + deriveWidth + spacing, rect.y, labelWidth, line);

                string display = string.IsNullOrEmpty(derivedB64)
                    ? "<no derived key>"
                    : (derivedB64.Length > 40 ? derivedB64.Substring(0, 40) + "..." : derivedB64);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.TextField(labelRect, GUIContent.none, display);
                }

                // Right: Copy button
                var copyRect = new Rect(labelRect.x + labelWidth + spacing, rect.y, button, line);
                if (GUI.Button(copyRect, "Copy"))
                {
                    if (!string.IsNullOrEmpty(derivedB64))
                    {
                        EditorGUIUtility.systemCopyBuffer = derivedB64;
                        FlowSaveLog.Info("Derived key copied to clipboard.");
                    }
                }
            }

            private static string ComputeDerivedAesBase64(
                SerializedProperty keyB64Prop,
                SerializedProperty keyBitsProp)
            {
                if (keyB64Prop == null || keyBitsProp == null)
                    return string.Empty;

                string baseB64 = keyB64Prop.stringValue;
                if (string.IsNullOrEmpty(baseB64))
                    return string.Empty;

                try
                {
                    byte[] baseKey = Convert.FromBase64String(baseB64);
                    if (baseKey.Length == 0)
                        return string.Empty;

                    var bitsEnum = GetKeyBits(keyBitsProp);
                    byte[] derived = KeyResolver.DeriveAesKey(baseKey, bitsEnum);
                    return derived.Length > 0 ? Convert.ToBase64String(derived) : string.Empty;
                }
                catch (FormatException)
                {
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    FlowSaveLog.Warning("Failed to derive AES key in editor: " + ex.Message);
                    return string.Empty;
                }
            }

            private static string ComputeDerivedHmacBase64(SerializedProperty keyB64Prop)
            {
                if (keyB64Prop == null)
                    return string.Empty;

                string baseB64 = keyB64Prop.stringValue;
                if (string.IsNullOrEmpty(baseB64))
                    return string.Empty;

                try
                {
                    byte[] baseKey = Convert.FromBase64String(baseB64);
                    if (baseKey.Length == 0)
                        return string.Empty;

                    byte[] derived = KeyResolver.DeriveHmacKey(baseKey, 32);
                    return derived.Length > 0 ? Convert.ToBase64String(derived) : string.Empty;
                }
                catch (FormatException)
                {
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    FlowSaveLog.Warning("Failed to derive HMAC key in editor: " + ex.Message);
                    return string.Empty;
                }
            }

            // ─────────────────────────────────────────────────────────
            // Base key row: [Key (Base64)] [Generate] [Copy]
            // ─────────────────────────────────────────────────────────

            private static void DrawKeyB64Row(
                SerializedProperty keyB64Prop,
                bool isAes,
                SerializedProperty keyBitsProp)
            {
                if (keyB64Prop == null)
                    return;

                float line = EditorGUIUtility.singleLineHeight;
                float buttonWidth = 80f;
                float spacing = 4f;

                var rowRect = GUILayoutUtility.GetRect(0f, line);

                // Label
                var labelRect = new Rect(rowRect.x, rowRect.y, EditorGUIUtility.labelWidth, line);
                EditorGUI.LabelField(labelRect, "Key (Base64)");

                float remainingWidth = rowRect.width - EditorGUIUtility.labelWidth;
                float fieldWidth = remainingWidth - (buttonWidth * 2f + spacing * 2f);

                float x = rowRect.x + EditorGUIUtility.labelWidth;

                // Text field
                var fieldRect = new Rect(x, rowRect.y, fieldWidth, line);
                keyB64Prop.stringValue = EditorGUI.TextField(fieldRect, GUIContent.none, keyB64Prop.stringValue);

                x += fieldWidth + spacing;

                // Generate button
                var genRect = new Rect(x, rowRect.y, buttonWidth, line);
                if (GUI.Button(genRect, "Generate"))
                {
                    GenerateBaseKey(keyB64Prop, isAes, keyBitsProp);
                }

                x += buttonWidth + spacing;

                // Copy button
                var copyRect = new Rect(x, rowRect.y, buttonWidth, line);
                if (GUI.Button(copyRect, "Copy"))
                {
                    EditorGUIUtility.systemCopyBuffer = keyB64Prop.stringValue ?? string.Empty;
                    FlowSaveLog.Info("Base key copied to clipboard.");
                }
            }

            private static void GenerateBaseKey(
                SerializedProperty keyB64Prop,
                bool isAes,
                SerializedProperty keyBitsProp)
            {
                int bytes = 32; // default for HMAC / generic

                if (isAes && keyBitsProp != null && keyBitsProp.propertyType == SerializedPropertyType.Enum)
                {
                    var bits = GetKeyBits(keyBitsProp);
                    bytes = (int)bits / 8;
                }

                if (bytes <= 0)
                    bytes = 32;

                var data = new byte[bytes];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(data);
                }

                keyB64Prop.stringValue = Convert.ToBase64String(data);
                FlowSaveLog.Info($"Generated {bytes}-byte base key.");
            }


            // ─────────────────────────────────────────────────────────
            // Small helper: set enum index by name
            // ─────────────────────────────────────────────────────────

            private static void SetEnumByName(SerializedProperty enumProp, string enumName)
            {
                if (enumProp == null || enumProp.propertyType != SerializedPropertyType.Enum)
                    return;

                var names = enumProp.enumNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] == enumName)
                    {
                        enumProp.enumValueIndex = i;
                        return;
                    }
                }
            }

            private static KeyBits GetKeyBits(SerializedProperty keyBitsProp)
            {
                if (keyBitsProp == null || keyBitsProp.propertyType != SerializedPropertyType.Enum)
                    return KeyBits._128; // sensible default

                string name = keyBitsProp.enumNames[keyBitsProp.enumValueIndex];
                try
                {
                    return (KeyBits)Enum.Parse(typeof(KeyBits), name);
                }
                catch
                {
                    return KeyBits._128;
                }
            }

        }
    }
}

#endif
