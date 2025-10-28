using Flowsave.Shared;
using UnityEngine;

namespace Flowsave.Configurations
{
    public sealed class DefaultAppEnvironment : IAppEnvironment
    {
        public AppMode GetCurrentMode(AppMode? forcedEditorMode = null)
        {
#if UNITY_EDITOR
            if (forcedEditorMode.HasValue)
                return forcedEditorMode.Value;

            return AppMode.Editor;
#elif Developement
            return AppMode.Development;
#else
            return AppMode.Release;
#endif
        }
    }
}