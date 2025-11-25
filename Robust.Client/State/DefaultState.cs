using Robust.Shared.ContentPack;

namespace Robust.Client.State
{
    // Dummy state that is only used to make sure there always is *a* state.
    [ContentAccessAllowed]
    public sealed class DefaultState : State
    {
        protected override void Startup()
        {
        }

        protected override void Shutdown()
        {
        }
    }
}
