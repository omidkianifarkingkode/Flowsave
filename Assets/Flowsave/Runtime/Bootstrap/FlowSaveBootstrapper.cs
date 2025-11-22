using Flowsave.Namespaces;
using Flowsave.Serialization;
using UnityEngine;

namespace Flowsave.Unity
{
    public sealed class FlowSaveBootstrapper : MonoBehaviour
    {
        [Header("Config Repository")]
        [SerializeField] private FlowSaveConfiguration configRepository;

        [Header("Environment")]
        [SerializeField] private bool overrideEditorMode;
        [SerializeField] private AppMode editorModeOverride = AppMode.Editor;

        public static IFlowSaveService Service { get; private set; }

        void Awake()
        {
            // Basic guard
            if (Service != null && Service is { })
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            // Create environment
            //IAppEnvironment appEnv = new DefaultAppEnvironment();

#if UNITY_EDITOR
            if (overrideEditorMode)
            {
                // simple wrapper to inject forced mode
              //  appEnv = new ForcedAppEnvironment(appEnv, editorModeOverride);
            }
#endif

            var serializerFactory = new SerializerFactory(default);

            //Service = new FlowSaveService(
            //    configRepository,
            //    appEnv,
            //    serializerFactory);

            Debug.Log("[FlowSaveBootstrapper] FlowSave initialized.");
        }
    }
}
