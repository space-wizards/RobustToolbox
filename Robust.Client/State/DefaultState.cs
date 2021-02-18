namespace Robust.Client.State
{
    // Dummy state that is only used to make sure there always is *a* state.
    public class DefaultState : State
    {
        public override void Startup()
        {
        }

        public override void Shutdown()
        {
        }
    }
}
