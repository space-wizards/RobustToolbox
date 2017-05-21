using SFML.System;
using SS14.Client.Interfaces.Configuration;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    public class TransformComponent : Component
    {
        private Vector2f _position = new Vector2f();
        private List<TransformComponentState> states = new List<TransformComponentState>();
        private TransformComponentState lastState;
        public TransformComponentState lerpStateFrom;
        public TransformComponentState lerpStateTo;
        public TransformComponent()
        {
            Family = ComponentFamily.Transform;
        }

        public Vector2f Position
        {
            get { return _position; }
            set
            {
                Vector2f oldPosition = _position;
                _position = value;

                if (OnMove != null) OnMove(this, new VectorEventArgs(oldPosition, _position));
            }
        }

        public override Type StateType
        {
            get { return typeof (TransformComponentState); }
        }

        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2f(value, Position.Y); }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2f(Position.X, value); }
        }

        public event EventHandler<VectorEventArgs> OnMove;

        public override void Shutdown()
        {
            Position = new Vector2f();
        }

        public void TranslateTo(Vector2f toPosition)
        {
            Position = toPosition;
        }

        public void TranslateByOffset(Vector2f offset)
        {
            Position = Position + offset;

        }

        public override void HandleComponentState(dynamic state)
        {
            SetNewState(state);
        }

        private void SetNewState(TransformComponentState state)
        {
            lastState = state;
            states.Add(state);
            var interp = IoCManager.Resolve<IPlayerConfigurationManager>().GetInterpolation();
            //Remove all states older than the one just before the interp time.
            lerpStateFrom = states.Where(s => s.ReceivedTime <= state.ReceivedTime - interp).OrderByDescending(s => s.ReceivedTime).FirstOrDefault();
            if (lerpStateFrom != null)
            {
                lerpStateTo =
                    states.Where(s => s.ReceivedTime > lerpStateFrom.ReceivedTime).OrderByDescending(s => s.ReceivedTime).
                        LastOrDefault();
                if (lerpStateTo == null)
                    lerpStateTo = lerpStateFrom;
                states.RemoveAll(s => s.ReceivedTime < lerpStateFrom.ReceivedTime);
            }
            else
            {
                lerpStateFrom = state;
                lerpStateTo = state;
            }
            if(lastState.ForceUpdate)
            {
                TranslateTo(new Vector2f(state.X, state.Y));
            }

        }
    }
}
