using Flowsave.Shared;

namespace Flowsave.Configurations
{
    // Optional helper to wrap forced mode
    public sealed class ForcedAppEnvironment : IAppEnvironment
    {
        private readonly IAppEnvironment _inner;
        private readonly AppMode _forced;

        public ForcedAppEnvironment(IAppEnvironment inner, AppMode forced)
        {
            _inner = inner;
            _forced = forced;
        }

        public AppMode GetCurrentMode(AppMode? forcedEditorMode = null)
            => _inner.GetCurrentMode(_forced);
    }
}