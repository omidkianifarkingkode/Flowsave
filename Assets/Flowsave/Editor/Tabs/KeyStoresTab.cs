using Flowsave.KeyStorage;
using FlowSave.Configurations;
using System;
using UnityEditor;
using UnityEngine;

namespace FlowSave.Editor
{
    public partial class FlowSaveConfigWindow
    {
        private class KeyStoresTab : IFlowSaveConfigTab
        {
            public string Title => "Key Stores";

            private Vector2 _scroll;

            public void Draw(SerializedObject config)
            {
                if (config == null) return;

                var storesProp = config.FindProperty(nameof(FlowSaveConfiguration.KeyStores));
                if (storesProp == null)
                {
                    EditorGUILayout.HelpBox("KeyStores list not found on FlowSaveConfiguration.", MessageType.Error);
                    return;
                }

                EditorGUILayout.LabelField("Key Stores (per App Mode)", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                for (int i = 0; i < storesProp.arraySize; i++)
                {
                    var item = storesProp.GetArrayElementAtIndex(i);
                    var modeProp = item.FindPropertyRelative(nameof(AppModeKeyStore.AppMode));
                    var keyStoreProp = item.FindPropertyRelative(nameof(AppModeKeyStore.KeyStore));

                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();

                    // ✅ FIXED: don't use enumValueIndex / enumDisplayNames
                    string label = $"KeyStore {i}";
                    if (modeProp != null && modeProp.propertyType == SerializedPropertyType.Enum)
                    {
                        // AppMode is [Flags], so use intValue → AppMode → ToString()
                        var raw = modeProp.intValue;
                        var mode = (AppMode)raw;

                        label = mode == AppMode.None
                            ? "KeyStore (None)"
                            : mode.ToString();   // e.g. "Editor, Development"
                    }

                    item.isExpanded = EditorGUILayout.Foldout(item.isExpanded, label, true);

                    if (GUILayout.Button("X", GUILayout.Width(22)))
                    {
                        storesProp.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (item.isExpanded)
                    {
                        EditorGUI.indentLevel++;

                        if (modeProp != null)
                            EditorGUILayout.PropertyField(modeProp, new GUIContent("App Mode"));

                        if (keyStoreProp != null)
                            FlowSaveKeyStoreDrawer.DrawKeyStore(keyStoreProp);

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
                if (GUILayout.Button("Add Key Store", GUILayout.Height(24)))
                {
                    int index = storesProp.arraySize;
                    storesProp.InsertArrayElementAtIndex(index);
                    var newStore = storesProp.GetArrayElementAtIndex(index);

                    var modeProp = newStore.FindPropertyRelative(nameof(AppModeKeyStore.AppMode));
                    if (modeProp != null)
                    {
                        // default to Editor, or AppMode.None if you prefer
                        modeProp.intValue = (int)AppMode.Editor;
                    }

                    newStore.isExpanded = true;
                }
            }
        }
    }
}
