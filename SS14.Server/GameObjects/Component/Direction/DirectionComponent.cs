using SFML.System;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Direction;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;

namespace SS14.Server.GameObjects
{
    public class DirectionComponent : Component, IDirectionComponent
    {
        public override string Name => "Direction";
        public override uint? NetID => NetIDs.DIRECTION;
        private Direction _lastDeterminedDirection = Direction.South;

        #region IDirectionComponent Members

        public Direction Direction { get; set; }

        #endregion IDirectionComponent Members

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            owner.GetComponent<TransformComponent>().OnMove += HandleOnMove;
        }

        public override void OnRemove()
        {
            Owner.GetComponent<TransformComponent>().OnMove -= HandleOnMove;
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

        public override ComponentState GetComponentState()
        {
            return new DirectionComponentState(Direction);
        }
    }
}
