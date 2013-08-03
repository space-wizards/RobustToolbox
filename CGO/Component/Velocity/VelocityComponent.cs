using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;
using GorgonLibrary;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Velocity;

namespace CGO
{
    public class VelocityComponent : Component
    {
        private Vector2D _velocity = Vector2D.Zero;

        private VelocityComponentState _previousState;
        private VelocityComponentState _lastState;

        public Vector2D Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }


        public VelocityComponent()
            : base()
        {
            Family = ComponentFamily.Velocity;
        }

        public override Type StateType
        {
            get { return typeof(VelocityComponentState); }
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
