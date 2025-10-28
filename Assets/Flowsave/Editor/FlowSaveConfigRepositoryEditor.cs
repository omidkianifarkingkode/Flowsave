// File: Flowsave/Configurations/Editor/FlowSaveConfigRepositoryEditor.cs
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Flowsave.Configurations.Editor
{
    [CustomEditor(typeof(FlowSaveConfigRepository))]
    public class FlowSaveConfigRepositoryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("namespaces"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultAsset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forceModeInEditor"));
            if (serializedObject.FindProperty("forceModeInEditor").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("forcedEditorMode"));
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Index"))
                {
                    ((FlowSaveConfigRepository)target).RebuildIndex();
                    EditorUtility.SetDirty(target);
                }
                if (GUILayout.Button("Refetch Namespaces (Scan Project)"))
                {
                    RefetchNamespaces((FlowSaveConfigRepository)target);
                }
                if (GUILayout.Button("Sort by Namespace"))
                {
                    SortNamespaces((FlowSaveConfigRepository)target);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void RefetchNamespaces(FlowSaveConfigRepository repo)
        {
            var guids = AssetDatabase.FindAssets("t:FlowSaveConfigAsset");
            var assets = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<FlowSaveConfigAsset>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();

            repo.SetNamespaces(assets);
            repo.RebuildIndex();
            EditorUtility.SetDirty(repo);
            Debug.Log($"FlowSave: Refetched {assets.Count} namespace assets into repository.");
        }

        void SortNamespaces(FlowSaveConfigRepository repo)
        {
            var sorted = repo.Namespaces
                .Where(a => a != null)
                .OrderBy(a => a.NamespaceId)
                .ToList();
            repo.SetNamespaces(sorted);
            EditorUtility.SetDirty(repo);
        }
    }
}
#endif
