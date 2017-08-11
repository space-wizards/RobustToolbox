using SFML.System;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    public class TransformComponent : Component, IClientTransformComponent
    {
        public override string Name => "Transform";
        public override uint? NetID => NetIDs.TRANSFORM;
        private Vector2f _position = new Vector2f();
        private List<TransformComponentState> states = new List<TransformComponentState>();
        private TransformComponentState lastState;
        public TransformComponentState lerpStateFrom { get; private set; }
        public TransformComponentState lerpStateTo { get; private set ; }

        public override Type StateType => typeof(TransformComponentState);

        #region ITransformComponent Members

        public event EventHandler<VectorEventArgs> OnMove;

        public Vector2f Position
        {
            get => _position;
            set
            {
                Vector2f oldPosition = _position;
                _position = value;

                OnMove?.Invoke(this, new VectorEventArgs(oldPosition, _position));
            }
        }

        public float X
        {
            get => Position.X;
            set => Position = new Vector2f(value, Position.Y);
        }

        public float Y
        {
            get => Position.Y;
            set => Position = new Vector2f(Position.X, value);
        }

        public void Offset(Vector2f offset)
        {
            Position += offset;
        }

        #endregion

        public override void Shutdown()
        {
            Position = new Vector2f();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (TransformComponentState)state;
            lastState = newState;
            states.Add(newState);
            var interp = IoCManager.Resolve<IConfigurationManager>().GetCVar<float>("net.interpolation");
            //Remove all states older than the one just before the interp time.
            lerpStateFrom = states.Where(s => s.ReceivedTime <= newState.ReceivedTime - interp).OrderByDescending(s => s.ReceivedTime).FirstOrDefault();
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
                lerpStateFrom = newState;
                lerpStateTo = newState;
            }
            if (lastState.ForceUpdate)
            {
                Position = newState.Position;
            }
        }
    }
}
