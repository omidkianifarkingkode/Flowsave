using FlowSave.Configurations;
using FlowSave.KeyStorage;
using FlowSave.Logging;
using System;
using UnityEngine;

namespace FlowSave
{
    /// <summary>
    /// Bootstraps FlowSave and exposes a global IFlowSave instance.
    /// Drop this on a GameObject in your first scene (or a dedicated bootstrap scene).
    /// </summary>
    public sealed class FlowSave : MonoBehaviour
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
        public static IFlowSave Instance { get; private set; }

        /// <summary>
        /// True if FlowSave has been initialized successfully.
        /// </summary>
        public static bool IsInitialized => Instance != null && _initialized;

        /// <summary>
        /// Optional global logger resolver. Use this when you need to create a logger
        /// with custom constructor parameters or from a DI container.
        /// </summary>
        public static Func<FlowSaveConfiguration, ILogger> GlobalLoggerResolver { get; set; }

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
            SetupLogger(configuration);

            // Create the FlowSave service
            Instance = new FlowSaveService(configuration);
            _initialized = true;

            // Keep this across scene loads
            DontDestroyOnLoad(gameObject);

            FlowSaveLog.Info("[FlowSaveBootstrapper] FlowSave initialized.");
        }

        private void SetupLogger(FlowSaveConfiguration config)
        {
            ILogger logger;

            // 1) If someone registered a global resolver, ask it
            if (GlobalLoggerResolver != null)
            {
                try
                {
                    var fromResolver = GlobalLoggerResolver(config);
                    if (fromResolver != null)
                        logger = fromResolver;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FlowSaveBootstrapper] GlobalLoggerResolver threw: {ex}");
                }
            }

            // 3) Fallback – create default UnityLogger
            logger = new UnityLogger(config.LoggingOptions);

            FlowSaveLog.SetLogger(logger);
        }
    }
}
