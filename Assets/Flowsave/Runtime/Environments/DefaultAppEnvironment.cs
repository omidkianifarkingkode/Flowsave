namespace Flowsave.Namespaces
{
    public sealed class DefaultAppEnvironment : IAppEnvironment
    {
        public EnvironmentMode GetCurrentMode(EnvironmentMode? forcedEditorMode = null)
        {
            if (forcedEditorMode.HasValue)
                return forcedEditorMode.Value;

#if UNITY_EDITOR
            return EnvironmentMode.Editor;
#elif DEVELOPMENT_BUILD
            return AppMode.Development;
#else
            return AppMode.Release;
#endif
        }
    }
}