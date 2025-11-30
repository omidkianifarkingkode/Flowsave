#if UNITY_EDITOR

using FlowSave;           
using FlowSave.Encryption;
using FlowSave.Logging;
using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace FlowSave.Editor
{
    [CustomPropertyDrawer(typeof(AesOptions))]
    public class AesOptionsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (!property.isExpanded)
                return line;

            // header + KeyBits + DeriveKey + KeyB64 row
            return line * 4f + spacing * 3f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Header / foldout
            var headerRect = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            var keyBitsProp = property.FindPropertyRelative(nameof(AesOptions.KeyBits));
            var deriveKeyProp = property.FindPropertyRelative(nameof(AesOptions.DeriveKey));
            var keyB64Prop = property.FindPropertyRelative(nameof(AesOptions.KeyB64));

            // Decide default key size based on field name (Aes128 vs Aes256)
            bool is256 = property.name.Contains("256");
            int bits = is256 ? 256 : 128;

            // Force KeyBits to match the field name and lock it
            if (keyBitsProp != null && keyBitsProp.propertyType == SerializedPropertyType.Enum)
            {
                string wantedName = is256 ? nameof(KeyBits._256) : nameof(KeyBits._128);

                for (int i = 0; i < keyBitsProp.enumDisplayNames.Length; i++)
                {
                    if (keyBitsProp.enumDisplayNames[i] == wantedName)
                    {
                        keyBitsProp.enumValueIndex = i;
                        break;
                    }
                }
            }

            float y = position.y + line + spacing;

            // Line 1: KeyBits (read-only)
            if (keyBitsProp != null)
            {
                var keyBitsRect = new Rect(position.x, y, position.width, line);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.PropertyField(keyBitsRect, keyBitsProp, new GUIContent("Key Size"));
                }
                y += line + spacing;
            }

            // Line 2: DeriveKey | DerivedKeyLabel | Copy
            if (deriveKeyProp != null)
            {
                var deriveRowRect = new Rect(position.x, y, position.width, line);
                DrawDeriveRow(deriveRowRect, deriveKeyProp, keyB64Prop, bits);
                y += line + spacing;
            }

            // Line 3: Base key (B64) + Generate + Copy
            var keyRowRect = new Rect(position.x, y, position.width, line);
            DrawKeyB64Row(keyRowRect, keyB64Prop, bits);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void DrawDeriveRow(
            Rect rowRect,
            SerializedProperty deriveKeyProp,
            SerializedProperty keyB64Prop,
            int bits)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float buttonWidth = 70f;
            float spacing = 4f;

            // Left: checkbox + label
            float deriveWidth = rowRect.width * 0.4f;

            var deriveRect = new Rect(rowRect.x, rowRect.y, deriveWidth, line);
            deriveKeyProp.boolValue = EditorGUI.ToggleLeft(deriveRect, "Derive Key", deriveKeyProp.boolValue);

            if (!deriveKeyProp.boolValue)
                return;

            // Compute derived key preview
            string derivedB64 = ComputeDerivedAesBase64(keyB64Prop, bits);

            // Middle: derived key label
            float labelWidth = rowRect.width - deriveWidth - buttonWidth - spacing * 2f;
            var labelRect = new Rect(rowRect.x + deriveWidth + spacing, rowRect.y, labelWidth, line);

            string display = string.IsNullOrEmpty(derivedB64)
                ? "<no derived key>"
                : (derivedB64.Length > 40 ? derivedB64.Substring(0, 40) + "..." : derivedB64);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.TextField(labelRect, GUIContent.none, display);
            }

            // Right: Copy button
            var copyRect = new Rect(labelRect.x + labelWidth + spacing, rowRect.y, buttonWidth, line);
            if (GUI.Button(copyRect, "Copy"))
            {
                if (!string.IsNullOrEmpty(derivedB64))
                {
                    EditorGUIUtility.systemCopyBuffer = derivedB64;
                    FlowSaveLog.Info("Derived AES key copied to clipboard.");
                }
            }
        }

        private string ComputeDerivedAesBase64(SerializedProperty keyB64Prop, int bits)
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

                var keyBits = bits == 256 ? KeyBits._256 : KeyBits._128;
                byte[] derived = KeyResolver.DeriveAesKey(baseKey, keyBits);
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

        private void DrawKeyB64Row(Rect rowRect, SerializedProperty keyB64Prop, int bits)
        {
            if (keyB64Prop == null)
                return;

            float line = EditorGUIUtility.singleLineHeight;
            float buttonWidth = 80f;
            float spacing = 4f;

            // Label on the left
            var labelRect = new Rect(rowRect.x, rowRect.y, EditorGUIUtility.labelWidth, line);
            EditorGUI.LabelField(labelRect, "Key (Base64)");

            // Remaining width for field + buttons
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
                GenerateKey(keyB64Prop, bits);
            }

            x += buttonWidth + spacing;

            // Copy button
            var copyRect = new Rect(x, rowRect.y, buttonWidth, line);
            if (GUI.Button(copyRect, "Copy"))
            {
                EditorGUIUtility.systemCopyBuffer = keyB64Prop.stringValue ?? string.Empty;
                FlowSaveLog.Info("AES base key copied to clipboard.");
            }
        }

        private void GenerateKey(SerializedProperty keyB64Prop, int bits)
        {
            int byteLen = bits / 8;
            var data = new byte[byteLen];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            keyB64Prop.stringValue = Convert.ToBase64String(data);
            FlowSaveLog.Info($"Generated {bits}-bit AES key.");
        }
    }
}
#endif
