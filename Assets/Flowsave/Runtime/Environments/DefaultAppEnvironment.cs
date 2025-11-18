using Flowsave.Shared;

namespace Flowsave.Configurations
{
    public sealed class DefaultAppEnvironment : IAppEnvironment
    {
        public AppMode GetCurrentMode(AppMode? forcedEditorMode = null)
        {
            if (forcedEditorMode.HasValue)
                return forcedEditorMode.Value;

#if UNITY_EDITOR
            return AppMode.Editor;
#elif DEVELOPMENT_BUILD
            return AppMode.Development;
#else
            return AppMode.Release;
#endif
        }
    }
}