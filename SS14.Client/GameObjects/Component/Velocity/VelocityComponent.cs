using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Velocity;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    public class VelocityComponent : ClientComponent
    {
        public override string Name => "Velocity";
        private VelocityComponentState _lastState;
        private VelocityComponentState _previousState;
        private Vector2f _velocity = new Vector2f();

        public VelocityComponent()
        {
            Family = ComponentFamily.Velocity;
            Velocity = new Vector2f(0, 0);
        }

        public Vector2f Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public override Type StateType
        {
            get { return typeof(VelocityComponentState); }
        }

        public float X
        {
            get { return Velocity.X; }
            set { Velocity = new Vector2f(value, Velocity.Y); }
        }

        public float Y
        {
            get { return Velocity.Y; }
            set { Velocity = new Vector2f(Velocity.X, value); }
        }

        public override void Shutdown()
        {
            Velocity = new Vector2f();
        }

        public override void HandleComponentState(dynamic state)
        {
            if (Owner.GetComponent<PlayerInputMoverComponent>(ComponentFamily.Mover) == null)
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
