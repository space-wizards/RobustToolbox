using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
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
        public override void OnAdd()
        {
            // This component requires that the entity has a KeyBindingInputComponent.
            if (!Owner.HasComponent<KeyBindingInputComponent>())
                Logger.Error($"[ECS] {Owner.Prototype.Name} - {nameof(PlayerInputMoverComponent)} requires {nameof(KeyBindingInputComponent)}. ");

            // This component requires that the entity has a PhysicsComponent.
            if (!Owner.HasComponent<PhysicsComponent>())
                Logger.Error($"[ECS] {Owner.Prototype.Name} - {nameof(PlayerInputMoverComponent)} requires {nameof(PhysicsComponent)}. ");

            base.OnAdd();
        }

        /// <summary>
        ///     Handles an incoming component message.
        /// </summary>
        /// <param name="owner">
        ///     Object that raised the event. If the event was sent over the network or from some unknown place,
        ///     this will be null.
        /// </param>
        /// <param name="message">Message that was sent.</param>
        public override void HandleMessage(object owner, ComponentMessage message)
        {
            base.HandleMessage(owner, message);

            switch (message)
            {
                case BoundKeyChangedMsg msg:
                    HandleKeyChange();
                    break;
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var transform = Owner.GetComponent<TransformComponent>();
            var physics = Owner.GetComponent<PhysicsComponent>();

            physics.LinearVelocity = _moveDir * (_run ? FastMoveSpeed : BaseMoveSpeed);
            transform.LocalRotation = (float)(_moveDir.LengthSquared > 0.001 ? _moveDir.GetDir() : Direction.South).ToAngle();

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
