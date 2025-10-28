// File: Flowsave/Configurations/Editor/FlowSaveInitializerWindow.cs
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Flowsave.Configurations.Editor
{
    public class FlowSaveInitializerWindow : EditorWindow
    {
        FlowSaveConfigRepository repo;
        string repoName = "FlowSaveConfigRepository";
        string defaultNamespaceId = "default";
        string createFolder = "Assets/FlowSaveConfigs";
        string newNamespaceId = "playerData";

        [MenuItem("Tools/FlowSave/Initializer")]
        public static void Open()
        {
            var win = GetWindow<FlowSaveInitializerWindow>("FlowSave Init");
            win.minSize = new Vector2(420, 320);
            win.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("FlowSave • Package Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            repo = (FlowSaveConfigRepository)EditorGUILayout.ObjectField("Repository", repo, typeof(FlowSaveConfigRepository), false);
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("Create Repo + Default Namespace", EditorStyles.boldLabel);
            repoName = EditorGUILayout.TextField("Repo Asset Name", repoName);
            defaultNamespaceId = EditorGUILayout.TextField("Default Namespace Id", defaultNamespaceId);
            createFolder = EditorGUILayout.TextField("Create In Folder", createFolder);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(defaultNamespaceId)))
            {
                if (GUILayout.Button("Create Repository + Default Namespace"))
                {
                    CreateRepoAndDefault();
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Locate / Select Repository", EditorStyles.boldLabel);
            if (GUILayout.Button("Find Repository Assets"))
            {
                var found = FindRepoAssets();
                if (found.Length == 1)
                {
                    repo = found[0];
                    Selection.activeObject = repo;
                    EditorGUIUtility.PingObject(repo);
                }
                else if (found.Length > 1)
                {
                    EditorUtility.DisplayDialog("FlowSave", $"Found {found.Length} repositories. Select one manually in the object field.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("FlowSave", "No repositories found.", "OK");
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Create Namespace (and add to repo)", EditorStyles.boldLabel);
            newNamespaceId = EditorGUILayout.TextField("Namespace Id", newNamespaceId);

            using (new EditorGUI.DisabledScope(repo == null || string.IsNullOrEmpty(newNamespaceId)))
            {
                if (GUILayout.Button("Create Namespace Asset"))
                {
                    CreateNamespaceAssetAndAdd(newNamespaceId);
                }
            }

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(repo == null))
            {
                if (GUILayout.Button("Refetch All Namespace Assets into Repo"))
                {
                    RefetchNamespacesIntoRepo();
                }
            }
        }

        void CreateRepoAndDefault()
        {
            var repoPath = EnsureFolder(createFolder);
            if (repoPath == null)
            {
                EditorUtility.DisplayDialog("FlowSave", "Folder must be inside the Assets directory.", "OK");
                return;
            }

            // Create default namespace asset
            var defaultAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(repoPath, $"{defaultNamespaceId}.asset"));
            var defAsset = ScriptableObject.CreateInstance<FlowSaveConfigAsset>();
            defAsset.name = defaultNamespaceId;
            defAsset.Model.namespaceId = defaultNamespaceId;
            defAsset.Model.EnsureAllModes();
            AssetDatabase.CreateAsset(defAsset, defaultAssetPath);

            // Create repository
            var repoAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(repoPath, $"{repoName}.asset"));
            var repoAsset = ScriptableObject.CreateInstance<FlowSaveConfigRepository>();
            repoAsset.SetDefaultAsset(defAsset);
            repoAsset.SetNamespaces(new System.Collections.Generic.List<FlowSaveConfigAsset> { defAsset });
            AssetDatabase.CreateAsset(repoAsset, repoAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            repo = repoAsset;
            Selection.activeObject = repo;
            EditorGUIUtility.PingObject(repo);

            Debug.Log($"FlowSave: Created repository at {repoAssetPath} with default namespace '{defaultNamespaceId}'.");
        }

        void CreateNamespaceAssetAndAdd(string ns)
        {
            var repoPath = GetRepoFolder(repo);
            if (repoPath == null)
            {
                EditorUtility.DisplayDialog("FlowSave", "Could not infer repository folder. Please place the repo asset under an Assets/ path.", "OK");
                return;
            }

            // Duplicate check
            repo.RebuildIndex();
            if (repo.TryGet(ns, out _))
            {
                EditorUtility.DisplayDialog("FlowSave", $"Namespace '{ns}' already exists in repo.", "OK");
                return;
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(repoPath, $"{ns}.asset"));
            var asset = ScriptableObject.CreateInstance<FlowSaveConfigAsset>();
            asset.name = ns;
            asset.Model.namespaceId = ns;
            asset.Model.EnsureAllModes();
            AssetDatabase.CreateAsset(asset, assetPath);

            // add to repo
            var list = repo.Namespaces;
            if (!list.Any(a => a && a.NamespaceId == ns))
            {
                list.Add(asset);
                repo.SetNamespaces(list);
                repo.RebuildIndex();
                EditorUtility.SetDirty(repo);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"FlowSave: Created namespace asset '{ns}' at {assetPath} and added to repo.");
        }

        void RefetchNamespacesIntoRepo()
        {
            var guids = AssetDatabase.FindAssets("t:FlowSaveConfigAsset");
            var assets = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<FlowSaveConfigAsset>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();

            repo.SetNamespaces(assets);
            repo.RebuildIndex();
            EditorUtility.SetDirty(repo);
            AssetDatabase.SaveAssets();

            Debug.Log($"FlowSave: Refetched {assets.Count} namespace assets into repository.");
        }

        FlowSaveConfigRepository[] FindRepoAssets()
        {
            var guids = AssetDatabase.FindAssets("t:FlowSaveConfigRepository");
            return guids
                .Select(g => AssetDatabase.LoadAssetAtPath<FlowSaveConfigRepository>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToArray();
        }

        string EnsureFolder(string desired)
        {
            // Must be under Assets/
            if (!desired.StartsWith("Assets"))
                desired = "Assets";

            if (!AssetDatabase.IsValidFolder(desired))
            {
                var parts = desired.Split('/');
                string curr = "Assets";
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = $"{curr}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(curr, parts[i]);
                    curr = next;
                }
            }
            return desired;
        }

        string GetRepoFolder(Object obj)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return null;
            var dir = Path.GetDirectoryName(path).Replace('\\', '/');
            return dir.StartsWith("Assets") ? dir : null;
        }
    }
}
#endif
