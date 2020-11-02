namespace Robust.Shared.GameObjects.Components.Eye
{
    public class SharedEyeComponent : Component
    {
        public override string Name => "Eye";
        public override uint? NetID => NetIDs.EYE;

        public virtual bool DrawFov { get; set; }

        public override ComponentState GetComponentState()
        {
            return new EyeComponentState(DrawFov);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is EyeComponentState state))
            {
                return;
            }

            DrawFov = state.DrawFov;
        }
    }
    
    public class EyeComponentState : ComponentState
    {
        public readonly bool DrawFov;
        
        public EyeComponentState(bool drawFov) : base(NetIDs.EYE)
        {
            DrawFov = drawFov;
        }
    }
}