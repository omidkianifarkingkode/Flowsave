#if UNITY_EDITOR

using FlowSave.Encryption;
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

            // header + KeyBits + DeriveKey + KeyB64
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

            // Line 1: KeyBits (read-only, labeled Key Size)
            if (keyBitsProp != null)
            {
                var keyBitsRect = new Rect(position.x, y, position.width, line);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.PropertyField(keyBitsRect, keyBitsProp, new GUIContent("Key Size"));
                }
                y += line + spacing;
            }

            // Line 2: DeriveKey
            if (deriveKeyProp != null)
            {
                var deriveRect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(deriveRect, deriveKeyProp);
                y += line + spacing;
            }

            // Line 3: KeyB64
            if (keyB64Prop != null)
            {
                var keyRect = new Rect(position.x, y, position.width, line);
                EditorGUI.PropertyField(keyRect, keyB64Prop, new GUIContent("Key (Base64)"));
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}

#endif
