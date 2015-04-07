using GorgonLibrary;
using SS14.Client.Interfaces.Configuration;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Transform;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    public class TransformComponent : Component
    {
        private Vector2D _position = Vector2D.Zero;
        private List<TransformComponentState> states = new List<TransformComponentState>();
        private TransformComponentState lastState;
        public TransformComponentState lerpStateFrom;
        public TransformComponentState lerpStateTo;
        public TransformComponent()
        {
            Family = ComponentFamily.Transform;
        }

        public Vector2D Position
        {
            get { return _position; }
            set
            {
                Vector2D oldPosition = _position;
                _position = value;

                if (OnMove != null) OnMove(this, new VectorEventArgs(Vector2TypeConverter.ToVector2(oldPosition), Vector2TypeConverter.ToVector2(_position)));
            }
        }

        public override Type StateType
        {
            get { return typeof (TransformComponentState); }
        }

        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2D(value, Position.Y); }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2D(Position.X, value); }
        }

        public event EventHandler<VectorEventArgs> OnMove;

        public override void Shutdown()
        {
            Position = Vector2D.Zero;
        }

        public void TranslateTo(Vector2D toPosition)
        {
            Position = toPosition;
        }

        public void TranslateByOffset(Vector2D offset)
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
            var interp = IoCManager.Resolve<IConfigurationManager>().GetInterpolation();
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
                TranslateTo(new Vector2D(state.X, state.Y));
            }

        }
    }
}