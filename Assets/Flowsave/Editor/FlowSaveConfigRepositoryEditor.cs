// File: Flowsave/Configurations/Editor/FlowSaveConfigRepositoryEditor.cs
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Flowsave.Configurations.Editor
{
    [CustomEditor(typeof(FlowSaveConfigRepository))]
    public class FlowSaveConfigRepositoryEditor : UnityEditor.Editor
    {
        SerializedProperty namespacesProp;
        SerializedProperty globalAssetProp;
        SerializedProperty forceModeInEditorProp;
        SerializedProperty forcedEditorModeProp;

        string newNamespaceName = "{template}"; // Default to the placeholder name

        void OnEnable()
        {
            // Cache serialized properties
            namespacesProp = serializedObject.FindProperty("namespaces");
            globalAssetProp = serializedObject.FindProperty("globalAsset");
            forceModeInEditorProp = serializedObject.FindProperty("forceModeInEditor");
            forcedEditorModeProp = serializedObject.FindProperty("forcedEditorMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Display fields for namespaces and global asset
            EditorGUILayout.PropertyField(namespacesProp, true);
            EditorGUILayout.PropertyField(globalAssetProp);

            EditorGUILayout.PropertyField(forceModeInEditorProp);
            if (forceModeInEditorProp.boolValue)
            {
                EditorGUILayout.PropertyField(forcedEditorModeProp);
            }

            EditorGUILayout.Space(8);

            // Create Namespace section
            EditorGUILayout.LabelField("Create Namespace", EditorStyles.boldLabel);
            newNamespaceName = EditorGUILayout.TextField("Namespace Name", newNamespaceName);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(newNamespaceName) || newNamespaceName == "{template}"))
            {
                if (GUILayout.Button("Create Namespace"))
                {
                    CreateNamespace((FlowSaveConfigRepository)target, newNamespaceName);
                }
            }

            EditorGUILayout.Space(8);
            // Remove the sort button as per the request
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
            }

            serializedObject.ApplyModifiedProperties();
        }

        void CreateNamespace(FlowSaveConfigRepository repo, string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName) || namespaceName == "{template}")
            {
                EditorUtility.DisplayDialog("FlowSave", "Namespace name cannot be empty or '{template}'.", "OK");
                return;
            }

            // Get the directory where the Resources folder is located
            string repoPath = GetRepoFolder(repo);
            if (repoPath == null)
            {
                EditorUtility.DisplayDialog("FlowSave", "Could not infer repository folder. Please place the repo asset under an Assets/ path.", "OK");
                return;
            }

            string namespaceConfigPath = Path.Combine(repoPath.Replace("/Resources", ""), $"{namespaceName}-NamespaceConfig.asset");
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(namespaceConfigPath);
            var asset = ScriptableObject.CreateInstance<FlowSaveConfigAsset>();
            asset.name = $"{namespaceName}-NamespaceConfig"; // Add postfix to the namespace config name
            asset.Model.namespaceId = namespaceName;
            asset.Model.EnsureAllModes();
            AssetDatabase.CreateAsset(asset, assetPath);

            // Add the newly created namespace config to the repo
            var list = repo.Namespaces;
            if (!list.Any(a => a && a.NamespaceId == namespaceName))
            {
                list.Add(asset);
                repo.SetNamespaces(list);
                repo.RebuildIndex();
                EditorUtility.SetDirty(repo);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"FlowSave: Created namespace asset '{namespaceName}-NamespaceConfig' at {assetPath} and added to repo.");
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

        // Helper to get the folder path of the repo
        string GetRepoFolder(FlowSaveConfigRepository repo)
        {
            var path = AssetDatabase.GetAssetPath(repo);
            if (string.IsNullOrEmpty(path)) return null;

            // Get the directory where the Resources folder is located
            var dir = Path.GetDirectoryName(path).Replace('\\', '/');
            return dir.StartsWith("Assets") ? dir : null;
        }
    }
}
#endif
