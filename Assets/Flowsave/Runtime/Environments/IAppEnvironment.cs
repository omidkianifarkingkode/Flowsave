using Flowsave.Shared;

namespace Flowsave.Configurations
{
    public interface IAppEnvironment
    {
        AppMode GetCurrentMode(AppMode? forcedEditorMode = null);
    }
}