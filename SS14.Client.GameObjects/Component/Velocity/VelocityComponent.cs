using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Velocity;
using System;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    public class VelocityComponent : Component
    {
        private VelocityComponentState _lastState;
        private VelocityComponentState _previousState;
        private Vector2 _velocity = Vector2.Zero;


        public VelocityComponent()
        {
            Family = ComponentFamily.Velocity;
            Velocity = new Vector2(0,0);
        }

        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public override Type StateType
        {
            get { return typeof (VelocityComponentState); }
        }

        public float X
        {
            get { return Velocity.X; }
            set { Velocity = new Vector2(value, Velocity.Y); }
        }

        public float Y
        {
            get { return Velocity.Y; }
            set { Velocity = new Vector2(Velocity.X, value); }
        }

        public override void Shutdown()
        {
            Velocity = Vector2.Zero;
        }

        public override void HandleComponentState(dynamic state)
        {
            if(Owner.GetComponent<PlayerInputMoverComponent>(ComponentFamily.Mover) == null)
                SetNewState(state);
        }

        private void SetNewState(VelocityComponentState state)
        {
            if (_lastState != null)
                _previousState = _lastState;
            _lastState = state;
            Velocity = new Vector2(state.VelocityX, state.VelocityY);
        }
    }
}