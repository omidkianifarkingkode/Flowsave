#if UNITY_EDITOR

using UnityEditor;

namespace FlowSave.Editor
{
    public partial class FlowSaveConfigWindow
    {
        // ─────────────────────────────────────────────────────────────
        //  TAB INTERFACE + IMPLEMENTATIONS
        // ─────────────────────────────────────────────────────────────

        private interface IFlowSaveConfigTab
        {
            string Title { get; }
            void Draw(SerializedObject config);
        }
    }
}

#endif
