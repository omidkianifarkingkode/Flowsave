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
        string globalNamespaceId = "global-game-data";
        string createFolder = "Assets/FlowSaveConfigs";
        string newNamespaceId = "{template}";

        [MenuItem("Tools/FlowSave/Initializer")]
        public static void Open()
        {
            var win = GetWindow<FlowSaveInitializerWindow>("FlowSave Init");
            win.minSize = new Vector2(420, 320);
            win.Show();
        }

        void OnEnable()
        {
            // Attempt to load the existing repo, if any
            if (repo == null)
            {
                var foundRepos = FindRepoAssets();
                if (foundRepos.Length == 1)
                {
                    repo = foundRepos[0];
                }
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("FlowSave • Package Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            // Display the repository object field
            repo = (FlowSaveConfigRepository)EditorGUILayout.ObjectField("Repository", repo, typeof(FlowSaveConfigRepository), false);
            EditorGUILayout.Space(6);

            // Disable button if repo already exists
            EditorGUILayout.LabelField("Create Repo + Global Namespace", EditorStyles.boldLabel);
            repoName = EditorGUILayout.TextField("Repo Asset Name", repoName);
            globalNamespaceId = EditorGUILayout.TextField("Global Namespace Id", globalNamespaceId);
            createFolder = EditorGUILayout.TextField("Create In Folder", createFolder);

            bool repoExists = repo != null;
            using (new EditorGUI.DisabledScope(repoExists || string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(globalNamespaceId)))
            {
                if (GUILayout.Button("Create Repository + Global Namespace"))
                {
                    CreateRepoAndGlobalNamespace();
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

            // Validate that the namespace is not empty or {template}
            using (new EditorGUI.DisabledScope(repo == null || string.IsNullOrEmpty(newNamespaceId) || newNamespaceId == "{template}"))
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

        void CreateRepoAndGlobalNamespace()
        {
            // Ensure the folder exists
            var repoPath = EnsureFolder(createFolder);
            if (repoPath == null)
            {
                EditorUtility.DisplayDialog("FlowSave", "Folder must be inside the Assets directory.", "OK");
                return;
            }

            // Ensure the Resources folder exists inside the repo folder
            var resourcesFolderPath = Path.Combine(repoPath, "Resources");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath))
            {
                AssetDatabase.CreateFolder(repoPath, "Resources");
            }

            // Create global namespace asset with postfix
            var globalNamespaceWithPostfix = $"{globalNamespaceId}-NamespaceConfig"; // Add postfix
            var globalAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(repoPath, $"{globalNamespaceWithPostfix}.asset"));
            var globalAsset = ScriptableObject.CreateInstance<FlowSaveConfigAsset>();
            globalAsset.name = globalNamespaceWithPostfix;
            globalAsset.Model.namespaceId = globalNamespaceWithPostfix;
            globalAsset.Model.EnsureAllModes();
            AssetDatabase.CreateAsset(globalAsset, globalAssetPath);

            // Create repository in the Resources folder (inside the folder created by user)
            var repoAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(resourcesFolderPath, $"{repoName}.asset"));
            var repoAsset = ScriptableObject.CreateInstance<FlowSaveConfigRepository>();
            repoAsset.SetGlobalAsset(globalAsset);
            repoAsset.SetNamespaces(new System.Collections.Generic.List<FlowSaveConfigAsset> { globalAsset });
            AssetDatabase.CreateAsset(repoAsset, repoAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Update repo field and selection
            repo = repoAsset;
            Selection.activeObject = repo;
            EditorGUIUtility.PingObject(repo);

            Debug.Log($"FlowSave: Created repository at {repoAssetPath} with global namespace '{globalNamespaceWithPostfix}'.");
        }

        void CreateNamespaceAssetAndAdd(string ns)
        {
            if (string.IsNullOrEmpty(ns) || ns == "{template}")
            {
                EditorUtility.DisplayDialog("FlowSave", "Namespace name cannot be empty or '{template}'.", "OK");
                return;
            }

            var repoPath = EnsureFolder(createFolder); // The folder path to create namespaces
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

            // Create namespace config file in the "Assets/FlowSaveConfigs/" folder (NOT in Resources)
            var namespaceConfigPath = Path.Combine(repoPath, $"{ns}-NamespaceConfig.asset");
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(namespaceConfigPath);
            var asset = ScriptableObject.CreateInstance<FlowSaveConfigAsset>();
            asset.name = $"{ns}-NamespaceConfig"; // Postfix added
            asset.Model.namespaceId = ns;
            asset.Model.EnsureAllModes();
            AssetDatabase.CreateAsset(asset, assetPath);

            // Add to repo
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

            Debug.Log($"FlowSave: Created namespace asset '{ns}-NamespaceConfig' at {assetPath} and added to repo.");
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
