#if UNITY_EDITOR

using FlowSave.Signing;
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

            // header + DeriveKey + KeyId + TruncateTo + KeyB64
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
            var keyIdProp = property.FindPropertyRelative(nameof(HmacOptions.KeyId));
            var truncateProp = property.FindPropertyRelative(nameof(HmacOptions.TruncateTo));
            var keyB64Prop = property.FindPropertyRelative(nameof(HmacOptions.KeyB64));

            float y = position.y + line + spacing;

            // Line 1: DeriveKey
            if (deriveKeyProp != null)
            {
                var rect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(rect, deriveKeyProp);
                y += line + spacing;
            }

            // Line 2: KeyId
            if (keyIdProp != null)
            {
                var rect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(rect, keyIdProp);
                y += line + spacing;
            }

            // Line 3: TruncateTo
            if (truncateProp != null)
            {
                var rect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(rect, truncateProp);
                y += line + spacing;
            }

            // Line 4: KeyB64
            if (keyB64Prop != null)
            {
                var rect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(rect, keyB64Prop, new GUIContent("Key (Base64)"));
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}

#endif
