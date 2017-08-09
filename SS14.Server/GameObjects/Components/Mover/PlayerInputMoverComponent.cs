using OpenTK;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects
{
    /// <summary>
    ///     Moves the entity based on input from a KeyBindingInputComponent.
    /// </summary>
    public class PlayerInputMoverComponent : Component, IMoverComponent
    {
        private const float BaseMoveSpeed = 4.0f;
        private const float FastMoveSpeed = 10.0f;

        private Vector2 _moveDir;
        private bool _run;

        /// <inheritdoc />
        public override string Name => "PlayerInputMover";

        /// <inheritdoc />
        public override uint? NetID => null;
        //public override uint? NetID => NetIDs.PLAYER_INPUT_MOVER;

        /// <inheritdoc />
        public override bool NetworkSynchronizeExistence => false;
        //public override bool NetworkSynchronizeExistence => true;

        /// <inheritdoc />
        public override void OnAdd(IEntity owner)
        {
            // This component requires that the entity has an AABB.
            if (!owner.HasComponent<KeyBindingInputComponent>())
                Logger.Error($"[ECS] {owner.Prototype.Name} - {nameof(PlayerInputMoverComponent)} requires {nameof(KeyBindingInputComponent)}. ");

            // This component requires that the entity has an AABB.
            if (!owner.HasComponent<PhysicsComponent>())
                Logger.Error($"[ECS] {owner.Prototype.Name} - {nameof(PlayerInputMoverComponent)} requires {nameof(PhysicsComponent)}. ");

            base.OnAdd(owner);
        }

        /// <inheritdoc />
        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            //Don't listen to our own messages!
            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.BoundKeyChange:
                    HandleKeyChange();
                    break;
            }

            return base.ReceiveMessage(sender, type, list);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var transform = Owner.GetComponent<TransformComponent>();
            var physics = Owner.GetComponent<PhysicsComponent>();

            var vel = _moveDir * (_run ? FastMoveSpeed : BaseMoveSpeed);
            physics.Velocity = vel;
            transform.Rotation = _moveDir.LengthSquared > 0.001 ? _moveDir : Direction.South.ToVec();

            base.Update(frameTime);
        }

        private void HandleKeyChange()
        {
            // receiving BoundKeyChange asserts KeyBindingInputComponent exists.
            var input = Owner.GetComponent<KeyBindingInputComponent>();

            var x = 0;
            x -= input.GetKeyState(BoundKeyFunctions.MoveLeft) ? 1 : 0;
            x += input.GetKeyState(BoundKeyFunctions.MoveRight) ? 1 : 0;

            var y = 0;
            y -= input.GetKeyState(BoundKeyFunctions.MoveDown) ? 1 : 0;
            y += input.GetKeyState(BoundKeyFunctions.MoveUp) ? 1 : 0;

            _moveDir = new Vector2(x, y);

            // can't normalize zero length vector
            if (_moveDir.LengthSquared > 0.0001)
                _moveDir.Normalize();

            // players can run or walk
            _run = input.GetKeyState(BoundKeyFunctions.Run);
        }
    }
}
