using FlowSave.Configurations;
using FlowSave.Logging;
using UnityEngine;

namespace FlowSave
{
    /// <summary>
    /// Bootstraps FlowSave and exposes a global IFlowSaveService instance.
    /// Drop this on a GameObject in your first scene (or a dedicated bootstrap scene).
    /// </summary>
    public sealed class FlowSave : MonoBehaviour
    {
        private static bool _initialized;

        [Header("Config Repository")]
        [SerializeField] private FlowSaveConfiguration configuration;

        [Header("Logging")]
        [SerializeReference] private ILogger logger;

        [Header("Environment")]
        [SerializeField] private bool overrideEditorMode;
        [SerializeField] private AppMode editorModeOverride = AppMode.Editor;

        /// <summary>
        /// Global FlowSave service instance.
        /// </summary>
        public static IFlowSave Instance { get; private set; }

        public ILogger Logger
        {
            get => logger;
            set => logger = value;
        }

        /// <summary>
        /// True if FlowSave has been initialized successfully.
        /// </summary>
        public static bool IsInitialized => Instance != null && _initialized;

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
                FlowSaveLog.Error("[FlowSaveBootstrapper] ConfigRepository is not assigned.");
                return;
            }

            // Optional: override AppMode in the editor for testing
#if UNITY_EDITOR
            if (overrideEditorMode)
            {
                FlowSaveConfiguration.ModeResolver = () => editorModeOverride;
            }
#endif

            KeyResolver.Initialize(SystemInfo.deviceUniqueIdentifier);

            // Create the FlowSave service
            Instance = new FlowSaveService(configuration, logger);
            _initialized = true;

            // Keep this across scene loads
            DontDestroyOnLoad(gameObject);

            FlowSaveLog.Info("[FlowSaveBootstrapper] FlowSave initialized.");
        }
    }
}
