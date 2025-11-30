#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using FlowSave.Configurations;

namespace FlowSave.Editor
{
    /// <summary>
    /// Main editor window for editing FlowSaveConfiguration assets.
    /// </summary>

    public partial class FlowSaveConfigWindow : EditorWindow
    {
        private const string WindowTitle = "FlowSave Configuration";

        private FlowSaveConfiguration _config;
        private SerializedObject _serializedConfig;

        private int _selectedTab;

        private IFlowSaveConfigTab[] _tabs;
        private string[] _tabTitles;

        [MenuItem("Window/FlowSave/Configuration")]
        public static void Open()
        {
            var window = GetWindow<FlowSaveConfigWindow>(false, WindowTitle, true);
            window.minSize = new Vector2(900f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(900f, 500f);
            FindOrAssignConfig();
            InitTabs();
        }

        private void InitTabs()
        {
            _tabs = new IFlowSaveConfigTab[]
            {
            new GlobalDefaultsTab(),
            new NamespacesTab(),
            new OtherSettingsTab(),
            };

            _tabTitles = new string[_tabs.Length];
            for (int i = 0; i < _tabs.Length; i++)
                _tabTitles[i] = _tabs[i].Title;
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                DrawNoConfigGUI();
                return;
            }

            if (_serializedConfig == null)
                _serializedConfig = new SerializedObject(_config);

            _serializedConfig.Update();

            EditorGUILayout.Space();

            DrawHeader();

            EditorGUILayout.Space();
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabTitles);
            EditorGUILayout.Space();

            if (_selectedTab >= 0 && _selectedTab < _tabs.Length)
            {
                _tabs[_selectedTab].Draw(_serializedConfig);
            }

            if (GUI.changed)
            {
                _serializedConfig.ApplyModifiedProperties();
                EditorUtility.SetDirty(_config);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField("Config Asset", _config, typeof(FlowSaveConfiguration), false);

            if (GUILayout.Button("Save", GUILayout.Width(80)))
            {
                SaveConfig();
            }

            EditorGUILayout.EndHorizontal();
        }

        #region Config detection / creation

        private void FindOrAssignConfig()
        {
            if (_config == null && Selection.activeObject is FlowSaveConfiguration selected)
            {
                SetConfig(selected);
                return;
            }

            var guids = AssetDatabase.FindAssets($"t:{nameof(FlowSaveConfiguration)}");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<FlowSaveConfiguration>(path);
                SetConfig(asset);
            }
        }

        private void SetConfig(FlowSaveConfiguration config)
        {
            _config = config;
            _serializedConfig = _config != null ? new SerializedObject(_config) : null;
        }

        private void DrawNoConfigGUI()
        {
            EditorGUILayout.HelpBox(
                "No FlowSaveConfiguration asset found in the project.",
                MessageType.Info);

            if (GUILayout.Button("Create FlowSaveConfiguration Asset", GUILayout.Height(28)))
            {
                CreateConfigAsset();
            }

            if (GUILayout.Button("Try Find Existing"))
            {
                FindOrAssignConfig();
            }
        }

        private void CreateConfigAsset()
        {
            var asset = CreateInstance<FlowSaveConfiguration>();

            var path = EditorUtility.SaveFilePanelInProject(
                "Create FlowSave Configuration",
                "FlowSaveConfiguration",
                "asset",
                "Choose where to save FlowSaveConfiguration asset");

            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(asset);
                return;
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            SetConfig(asset);
        }

        private void SaveConfig()
        {
            if (_config == null) return;

            _serializedConfig.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FlowSave] Configuration saved.");
        }

        #endregion
    }
}
#endif
