using OpenTK;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Server.GameObjects
{
    /// <summary>
    ///     Moves the entity based on input from a KeyBindingInputComponent.
    /// </summary>
    public class PlayerInputMoverComponent : Component, IMoverComponent
    {
        private const float BaseMoveSpeed = 4.0f;
        private const float FastMoveSpeed = 10.0f;

        private Direction LastDir = Direction.South;
        private Vector2 _moveDir;
        private bool _run;

        /// <inheritdoc />
        public override string Name => "PlayerInputMover";

        /// <inheritdoc />
        public override uint? NetID => null;

        /// <inheritdoc />
        public override bool NetworkSynchronizeExistence => false;

        /// <inheritdoc />
        public override void OnAdd(IEntity owner)
        {
            // This component requires that the entity has a KeyBindingInputComponent.
            if (!owner.HasComponent<KeyBindingInputComponent>())
                Logger.Error($"[ECS] {owner.Prototype.Name} - {nameof(PlayerInputMoverComponent)} requires {nameof(KeyBindingInputComponent)}. ");

            // This component requires that the entity has a PhysicsComponent.
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

            physics.Velocity = _moveDir * (_run ? FastMoveSpeed : BaseMoveSpeed);
            transform.Rotation = LastDir.ToAngle();

            base.Update(frameTime);
        }

        private void HandleKeyChange()
        {
            var input = Owner.GetComponent<KeyBindingInputComponent>();

            // key directions are in screen coordinates
            // _moveDir is in world coordinates
            // if the camera is moved, this needs to be changed

            var x = 0;
            x -= input.GetKeyState(BoundKeyFunctions.MoveLeft) ? 1 : 0;
            x += input.GetKeyState(BoundKeyFunctions.MoveRight) ? 1 : 0;

            var y = 0;
            y += input.GetKeyState(BoundKeyFunctions.MoveDown) ? 1 : 0;
            y -= input.GetKeyState(BoundKeyFunctions.MoveUp) ? 1 : 0;

            _moveDir = new Vector2(x, y);

            // can't normalize zero length vector
            if (_moveDir.LengthSquared > 1.0e-6)
                _moveDir = _moveDir.Normalized;

            if (_moveDir.LengthSquared > 0.001)
            {
                LastDir = _moveDir.GetDir();
            }

            // players can run or walk
            _run = input.GetKeyState(BoundKeyFunctions.Run);
        }
    }
}
