#if UNITY_EDITOR

using FlowSave.Logging;
using FlowSave.Signing;
using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace FlowSave.Editor
{
    [CustomPropertyDrawer(typeof(HmacOptions))]
    public class HmacOptionsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (!property.isExpanded)
                return line;

            // header + DeriveKey + KeyId + TruncateTo + KeyB64 row
            return line * 5f + spacing * 4f;
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

            var deriveKeyProp = property.FindPropertyRelative(nameof(HmacOptions.DeriveKey));
            var keyB64Prop = property.FindPropertyRelative(nameof(HmacOptions.KeyB64));
            var keyIdProp = property.FindPropertyRelative(nameof(HmacOptions.KeyId));
            var truncateProp = property.FindPropertyRelative(nameof(HmacOptions.TruncateTo));

            float y = position.y + line + spacing;

            // Line 1: DeriveKey | DerivedKeyLabel | Copy
            if (deriveKeyProp != null)
            {
                var deriveRowRect = new Rect(position.x, y, position.width, line);
                DrawDeriveRow(deriveRowRect, deriveKeyProp, keyB64Prop);
                y += line + spacing;
            }

            // Line 2: KeyId
            if (keyIdProp != null)
            {
                var keyIdRect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(keyIdRect, keyIdProp);
                y += line + spacing;
            }

            // Line 3: TruncateTo
            if (truncateProp != null)
            {
                var truncRect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(truncRect, truncateProp);
                y += line + spacing;
            }

            // Line 4: Base key (B64) + Generate + Copy
            var keyRowRect = new Rect(position.x, y, position.width, line);
            DrawKeyRow(keyRowRect, keyB64Prop);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void DrawDeriveRow(Rect rowRect, SerializedProperty deriveKeyProp, SerializedProperty keyB64Prop)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float buttonWidth = 70f;
            float spacing = 4f;

            // Left: checkbox + label
            // Use about 40% of the row for the toggle
            float deriveWidth = rowRect.width * 0.4f;

            var deriveRect = new Rect(rowRect.x, rowRect.y, deriveWidth, line);
            deriveKeyProp.boolValue = EditorGUI.ToggleLeft(deriveRect, "Derive Key", deriveKeyProp.boolValue);

            if (!deriveKeyProp.boolValue)
                return;

            // Compute derived key (preview only, does not touch serialized data)
            string derivedB64 = ComputeDerivedHmacBase64(keyB64Prop);

            // Middle: derived key label (truncated)
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
                    FlowSaveLog.Info("Derived HMAC key copied to clipboard.");
                }
            }
        }

        private string ComputeDerivedHmacBase64(SerializedProperty keyB64Prop)
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

                // 32 bytes is canonical for HMAC-SHA256
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

        private void DrawKeyRow(Rect rowRect, SerializedProperty keyB64Prop)
        {
            if (keyB64Prop == null)
                return;

            float line = EditorGUIUtility.singleLineHeight;
            float buttonWidth = 80f;
            float spacing = 4f;

            // Label
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
                GenerateKey(keyB64Prop);
            }

            x += buttonWidth + spacing;

            // Copy button
            var copyRect = new Rect(x, rowRect.y, buttonWidth, line);
            if (GUI.Button(copyRect, "Copy"))
            {
                EditorGUIUtility.systemCopyBuffer = keyB64Prop.stringValue ?? string.Empty;
                FlowSaveLog.Info("HMAC base key copied to clipboard.");
            }
        }

        private void GenerateKey(SerializedProperty keyB64Prop)
        {
            const int keyBytes = 32;
            var data = new byte[keyBytes];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            keyB64Prop.stringValue = Convert.ToBase64String(data);
            FlowSaveLog.Info($"Generated {keyBytes}-byte HMAC key.");
        }
    }
}
#endif
