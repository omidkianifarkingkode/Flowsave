#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public partial class FlowSaveConfigWindow
{
    /// <summary>
    /// Tab: other settings (dependency resolver, etc.).
    /// </summary>
    private class OtherSettingsTab : IFlowSaveConfigTab
    {
        public string Title => "Other Settings";

        private Vector2 _scroll;

        // Describe dependencies here
        private readonly DependencyInfo[] _dependencies =
        {
            new(
                displayName: "Protobuf-net",
                defineSymbol: "FLOWSAVE_PROTOBUF_NET",
                requiredTypes: new[] { "ProtoBuf.Serializer" },
                description: "https://github.com/protobuf-net/protobuf-net"
            ),
            new(
                displayName: "MessagePack",
                defineSymbol: "FLOWSAVE_MESSAGEPACK",
                requiredTypes: new[] { "MessagePack.MessagePackSerializer" },
                description: "https://github.com/neuecc/MessagePack-CSharp"
            ),
            new(
                displayName: "LZ4",
                defineSymbol: "FLOWSAVE_LZ4",
                requiredTypes: new[] { "LZ4.LZ4Codec" },
                description: "https://github.com/MiloszKrajewski/lz4net"
            ),
            new(
                displayName: "UniTask",
                defineSymbol: "FLOWSAVE_UNITASK",
                requiredTypes: new[] { "Cysharp.Threading.Tasks.UniTask" },
                description: "https://github.com/Cysharp/UniTask"
            ),
        };


        public void Draw(SerializedObject config)
        {
            EditorGUILayout.LabelField("Other Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawDependencyResolver();
        }

        // ─────────────────────────────────────────────────────────
        //  Dependency Resolver UI
        // ─────────────────────────────────────────────────────────
        private void DrawDependencyResolver()
        {
            EditorGUILayout.LabelField("Dependency Resolver", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These dependencies are optional features (Protobuf, MessagePack, LZ4, UniTask, etc.).\n" +
                "Status is based on whether the DLL types can be found and/or the scripting define symbol is set.",
                MessageType.Info);

            EditorGUILayout.Space();

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var symbols = GetDefineSymbols(group);
            var symbolSet = new HashSet<string>(symbols);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"Active Build Target Group: {group}", EditorStyles.miniLabel);
                EditorGUILayout.Space();

                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                foreach (var dep in _dependencies)
                {
                    DrawDependencyRow(dep, group, symbolSet);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndScrollView();
            }

            // If any symbols changed inside the rows, write them back once
            SetDefineSymbols(group, symbolSet);
        }

        private void DrawDependencyRow(DependencyInfo dep, BuildTargetGroup group, HashSet<string> symbols)
        {
            bool hasDefine = symbols.Contains(dep.DefineSymbol);
            bool hasTypes = dep.HasRequiredTypes();

            bool installed = hasTypes && hasDefine;
            bool notInstalled = !hasTypes && !hasDefine;

            string statusText =
                installed ? "Installed" :
                (hasTypes && !hasDefine) ? "DLL found, define missing" :
                (!hasTypes && hasDefine) ? "Define set, DLL missing" :
                "Not installed";

            var statusStyle = new GUIStyle(EditorStyles.label);

            if ((hasTypes && !hasDefine) || (!hasTypes && hasDefine))
            {
                statusStyle.normal.textColor = Color.yellow;
                statusStyle.fontStyle = FontStyle.Bold;
            }

            if (installed)
            {
                statusStyle.normal.textColor = Color.green;
                statusStyle.fontStyle = FontStyle.Bold;
            }

            if (notInstalled)
            {
                statusStyle.normal.textColor = Color.red;
                statusStyle.fontStyle = FontStyle.Bold;
            }

            EditorGUILayout.BeginHorizontal();

            // Name
            EditorGUILayout.LabelField(dep.DisplayName, GUILayout.Width(140));

            // Status
            EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Width(170));

            // Clickable Link
            if (!string.IsNullOrEmpty(dep.Description))
            {
                DrawLinkLabel(dep.Description, dep.Description, 260);
            }
            else
            {
                GUILayout.Space(260);
            }

            // PUSH buttons to right edge
            GUILayout.FlexibleSpace();

            // Resolve button (only if missing or partially installed)
            if (!installed)
            {
                if (GUILayout.Button("Resolve", GUILayout.Width(90)))
                {
                    OnResolveDependency(dep, symbols, hasTypes, hasDefine);
                }
            }

            // Remove button (only if define exists)
            if (hasDefine)
            {
                if (GUILayout.Button("Remove", GUILayout.Width(90)))
                {
                    symbols.Remove(dep.DefineSymbol);
                    Debug.Log($"[FlowSave] Removed define symbol: {dep.DefineSymbol}");
                }
            }

            EditorGUILayout.EndHorizontal();
        }



        private bool DrawLinkLabel(string label, string url, float width)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.5f, 1f) }, // link blue
                fontStyle = FontStyle.Italic
            };

            // Underline workaround: draw label manually
            var rect = GUILayoutUtility.GetRect(new GUIContent(label), style, GUILayout.Width(width));

            EditorGUI.LabelField(rect, label, style);

            // Draw underline
            var underlineRect = new Rect(rect.x, rect.yMax - 1, rect.width, 1);
            EditorGUI.DrawRect(underlineRect, style.normal.textColor);

            // Detect click
            if (Event.current.type == EventType.MouseUp &&
                rect.Contains(Event.current.mousePosition))
            {
                Application.OpenURL(url);
                return true;
            }

            // Show correct cursor
            if (Event.current.type == EventType.Repaint)
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            return false;
        }



        private void OnResolveDependency(
            DependencyInfo dep,
            HashSet<string> symbols,
            bool hasTypes,
            bool hasDefine)
        {
            if (!hasTypes)
            {
                // DLL not found – just inform the user
                EditorUtility.DisplayDialog(
                    "Dependency Not Found",
                    $"Could not find required types for '{dep.DisplayName}'.\n\n" +
                    "Make sure the DLL/package is added to your Unity project (via .dll, UPM, NuGet For Unity, etc.) " +
                    "and then click Resolve again.\n\n" +
                    $"Expected types:\n- {string.Join("\n- ", dep.RequiredTypes)}",
                    "OK");
                return;
            }

            // Types are present → ensure define symbol is set
            if (!symbols.Contains(dep.DefineSymbol))
            {
                symbols.Add(dep.DefineSymbol);
                Debug.Log($"[FlowSave] Added define symbol: {dep.DefineSymbol}");
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Define symbol helpers
        // ─────────────────────────────────────────────────────────
        private static string[] GetDefineSymbols(BuildTargetGroup group)
        {
            var raw = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (string.IsNullOrEmpty(raw))
                return Array.Empty<string>();

            return raw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrEmpty(s))
                      .ToArray();
        }

        private static void SetDefineSymbols(BuildTargetGroup group, HashSet<string> symbols)
        {
            var joined = string.Join(";", symbols.OrderBy(s => s));
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, joined);
        }

        // ─────────────────────────────────────────────────────────
        //  Small helper model
        // ─────────────────────────────────────────────────────────
        private class DependencyInfo
        {
            public string DisplayName { get; }
            public string DefineSymbol { get; }
            public string[] RequiredTypes { get; }
            public string Description { get; }

            public DependencyInfo(string displayName, string defineSymbol, string[] requiredTypes, string description = "")
            {
                DisplayName = displayName;
                DefineSymbol = defineSymbol;
                RequiredTypes = requiredTypes ?? Array.Empty<string>();
                Description = description;
            }

            public bool HasRequiredTypes()
            {
                if (RequiredTypes == null || RequiredTypes.Length == 0)
                    return true;

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var typeName in RequiredTypes)
                {
                    foreach (var asm in assemblies)
                    {
                        Type t = asm.GetType(typeName, false);
                        if (t != null)
                            return true;
                    }
                }

                return false;
            }
        }

    }
}

#endif
