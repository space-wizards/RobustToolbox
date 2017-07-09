using SFML.System;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Direction;
using SS14.Shared.Maths;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    public class DirectionComponent : ClientComponent
    {
        public override string Name => "Direction";
        private Direction _lastDeterminedDirection = Direction.South;

        public DirectionComponent()
        {
            Direction = Direction.South;
            Family = ComponentFamily.Direction;
        }

        public Direction Direction { get; set; }

        public override Type StateType
        {
            get { return typeof(DirectionComponentState); }
        }

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += HandleOnMove;
        }

        public override void OnRemove()
        {
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= HandleOnMove;
            base.OnRemove();
        }

        public void HandleOnMove(object sender, VectorEventArgs args)
        {
            if (!(Owner.GetComponent(ComponentFamily.Mover) is PlayerInputMoverComponent))
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
            if (delta.Length() < 0.001)
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

        public override void HandleComponentState(dynamic state)
        {
            var dir = (Direction)state.Direction;
            SetMoveDir(dir);
        }
    }
}
