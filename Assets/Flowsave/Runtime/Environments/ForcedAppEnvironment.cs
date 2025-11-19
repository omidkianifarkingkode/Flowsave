namespace Flowsave.Namespaces
{
    // Optional helper to wrap forced mode
    public sealed class ForcedAppEnvironment : IAppEnvironment
    {
        private readonly IAppEnvironment _inner;
        private readonly EnvironmentMode _forced;

        public ForcedAppEnvironment(IAppEnvironment inner, EnvironmentMode forced)
        {
            _inner = inner;
            _forced = forced;
        }

        public EnvironmentMode GetCurrentMode(EnvironmentMode? forcedEditorMode = null)
            => _inner.GetCurrentMode(_forced);
    }
}