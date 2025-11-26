using Flowsave;
using Flowsave.Configurations;
using UnityEngine;

namespace Flowsave.Unity
{
    /// <summary>
    /// Bootstraps FlowSave and exposes a global IFlowSaveService instance.
    /// Drop this on a GameObject in your first scene (or a dedicated bootstrap scene).
    /// </summary>
    public sealed class FlowSaveBootstrapper : MonoBehaviour
    {
        private static bool _initialized;

        [Header("Config Repository")]
        [SerializeField] private FlowSaveConfiguration configuration;

        [Header("Environment")]
        [SerializeField] private bool overrideEditorMode;
        [SerializeField] private AppMode editorModeOverride = AppMode.Editor;

        /// <summary>
        /// Global FlowSave service instance.
        /// </summary>
        public static IFlowSave Service { get; private set; }

        /// <summary>
        /// True if FlowSave has been initialized successfully.
        /// </summary>
        public static bool IsInitialized => Service != null && _initialized;

        private void Awake()
        {
            // Simple singleton guard
            if (_initialized)
            {
                // If a second bootstrapper appears, destroy it.
                Destroy(gameObject);
                return;
            }

            if (configuration == null)
            {
                Debug.LogError("[FlowSaveBootstrapper] ConfigRepository is not assigned.");
                return;
            }

            // Optional: override AppMode in the editor for testing
#if UNITY_EDITOR
            if (overrideEditorMode)
            {
                FlowSaveConfiguration.ModeResolver = () => editorModeOverride;
            }
#endif

            // Create the FlowSave service
            Service = new FlowSaveService(configuration);
            _initialized = true;

            // Keep this across scene loads
            DontDestroyOnLoad(gameObject);

            Debug.Log("[FlowSaveBootstrapper] FlowSave initialized.");
        }
    }
}
