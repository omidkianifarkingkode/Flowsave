namespace Flowsave.Namespaces
{
    public interface IAppEnvironment
    {
        EnvironmentMode GetCurrentMode(EnvironmentMode? forcedEditorMode = null);
    }
}