using GorgonLibrary;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Velocity;
using System;

namespace SS14.Client.GameObjects
{
    public class VelocityComponent : Component
    {
        private VelocityComponentState _lastState;
        private VelocityComponentState _previousState;
        private Vector2D _velocity = Vector2D.Zero;


        public VelocityComponent()
        {
            Family = ComponentFamily.Velocity;
            Velocity = new Vector2D(0,0);
        }

        public Vector2D Velocity
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
            set { Velocity = new Vector2D(value, Velocity.Y); }
        }

        public float Y
        {
            get { return Velocity.Y; }
            set { Velocity = new Vector2D(Velocity.X, value); }
        }

        public override void Shutdown()
        {
            Velocity = Vector2D.Zero;
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
            Velocity = new Vector2D(state.VelocityX, state.VelocityY);
        }
    }
}