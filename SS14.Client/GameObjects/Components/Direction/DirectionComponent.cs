using SFML.System;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Direction;
using SS14.Shared.Maths;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    public class DirectionComponent : ClientComponent, IDirectionComponent
    {
        public override string Name => "Direction";
        public override uint? NetID => NetIDs.DIRECTION;
        private Direction _lastDeterminedDirection = Direction.South;
        public Direction Direction { get; set; } = Direction.South;

        public override Type StateType => typeof(DirectionComponentState);

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            owner.GetComponent<ITransformComponent>().OnMove += HandleOnMove;
        }

        public override void OnRemove()
        {
            Owner.GetComponent<ITransformComponent>().OnMove -= HandleOnMove;
            base.OnRemove();
        }

        public void HandleOnMove(object sender, VectorEventArgs args)
        {
            if (!Owner.HasComponent<PlayerInputMoverComponent>())
            {
                return;
            }

            if (args.VectorFrom == args.VectorTo)
                return;
            SetMoveDir(DetermineDirection(args.VectorFrom, args.VectorTo));
        }

        private Direction DetermineDirection(Vector2f from, Vector2f to)
        {
            Vector2f delta = to - from;
            if (delta.Length() < 0.1)
            {
                return _lastDeterminedDirection;
            }

            _lastDeterminedDirection = from.DirectionTo(to, fallback: _lastDeterminedDirection);
            return _lastDeterminedDirection;
        }

        private void SetMoveDir(Direction movedir)
        {
            Direction = movedir;
            Owner.SendMessage(this, ComponentMessageType.MoveDirection, movedir);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (DirectionComponentState) state;
            var dir = newState.Direction;
            SetMoveDir(dir);
        }
    }
}
