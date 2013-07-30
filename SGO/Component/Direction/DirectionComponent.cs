using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Direction;
using ServerInterfaces.GameObject;

namespace SGO
{
    public class DirectionComponent : GameObjectComponent, IDirectionComponent
    {
        public Direction Direction { get; set; }
        public DirectionComponent()
            : base()
        {
            Direction = Direction.South;
            Family = ComponentFamily.Direction;
        }

        public override void OnAdd(GameObject.IEntity owner)
        {
            base.OnAdd(owner);
            owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += HandleOnMove;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= HandleOnMove;
        }

        public void HandleOnMove(object sender, VectorEventArgs args)
        {
            if ((args.VectorTo-args.VectorFrom).Magnitude < 0.1f)
                return;
            SetMoveDir(DetermineDirection(args.VectorFrom, args.VectorTo));
        }

        private Direction DetermineDirection(Vector2 from, Vector2 to)
        {
            var delta = to - from;
            if (delta.X > 0 && delta.Y > 0)
                return Direction.SouthEast;
            if (delta.X > 0 && delta.Y < 0)
                return Direction.NorthEast;
            if (delta.X < 0 && delta.Y > 0)
                return Direction.SouthWest;
            if (delta.X < 0 && delta.Y < 0)
                return Direction.NorthWest;
            if (delta.X > 0 && delta.Y == 0)
                return Direction.East;
            if (delta.X < 0 && delta.Y == 0)
                return Direction.West;
            if (delta.Y > 0 && delta.X == 0)
                return Direction.South;
            if (delta.Y < 0 && delta.X == 0)
                return Direction.North;
            return Direction.South;
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
