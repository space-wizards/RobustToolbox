using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Velocity;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    public class VelocityComponent : ClientComponent, IVelocityComponent
    {
        public override string Name => "Velocity";
        public override uint? NetID => NetIDs.VELOCITY;

        private VelocityComponentState _lastState;
        private VelocityComponentState _previousState;

        public Vector2f Velocity { get; set; }

        public override Type StateType => typeof(VelocityComponentState);

        public float X
        {
            get => Velocity.X;
            set => Velocity = new Vector2f(value, Velocity.Y);
        }

        public float Y
        {
            get => Velocity.Y;
            set => Velocity = new Vector2f(Velocity.X, value);
        }

        public override void Shutdown()
        {
            Velocity = new Vector2f();
        }

        public override void HandleComponentState(dynamic state)
        {
            if (!Owner.HasComponent<PlayerInputMoverComponent>())
                SetNewState(state);
        }

        private void SetNewState(VelocityComponentState state)
        {
            if (_lastState != null)
                _previousState = _lastState;
            _lastState = state;
            Velocity = new Vector2f(state.VelocityX, state.VelocityY);
        }
    }
}
