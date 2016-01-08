using SS14.Server.Interfaces.GOC;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Direction;
using System;
using SS14.Shared.Maths;
using SFML.System;

namespace SS14.Server.GameObjects
{
    public class DirectionComponent : Component, IDirectionComponent
    {
        private Direction _lastDeterminedDirection = Direction.South;

        public DirectionComponent()
        {
            Direction = Direction.South;
            Family = ComponentFamily.Direction;
        }

        #region IDirectionComponent Members

        public Direction Direction { get; set; }

        #endregion

        public override void OnAdd(Entity owner)
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
            if ((args.VectorTo - args.VectorFrom).Length() < 0.1f)
                return;
            SetMoveDir(DetermineDirection(args.VectorFrom, args.VectorTo));
        }

        private Direction DetermineDirection(Vector2f from, Vector2f to)
        {
            Vector2f delta = to - from;
            if (delta.Length() < 0.1f)
                return _lastDeterminedDirection;

            if (delta.X > 0 && delta.Y > 0)
                _lastDeterminedDirection = Direction.SouthEast;
            if (delta.X > 0 && delta.Y < 0)
                _lastDeterminedDirection = Direction.NorthEast;
            if (delta.X < 0 && delta.Y > 0)
                _lastDeterminedDirection = Direction.SouthWest;
            if (delta.X < 0 && delta.Y < 0)
                _lastDeterminedDirection = Direction.NorthWest;
            if (delta.X > 0 && Math.Abs(0-delta.Y) < 0.05f)
                _lastDeterminedDirection = Direction.East;
            if (delta.X < 0 && Math.Abs(0 - delta.Y) < 0.05f)
                _lastDeterminedDirection = Direction.West;
            if (delta.Y > 0 && Math.Abs(0 - delta.X) < 0.05f)
                _lastDeterminedDirection = Direction.South;
            if (delta.Y < 0 && Math.Abs(0 - delta.X) < 0.05f)
                _lastDeterminedDirection = Direction.North;
            return _lastDeterminedDirection;
        }

        private void SetMoveDir(Direction movedir)
        {
            Direction = movedir;
            Owner.SendMessage(this, ComponentMessageType.MoveDirection, movedir);
        }

        public override ComponentState GetComponentState()
        {
            return new DirectionComponentState(Direction);
        }
    }
}