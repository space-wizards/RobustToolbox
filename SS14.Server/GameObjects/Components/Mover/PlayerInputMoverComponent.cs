using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
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
        private bool _movingUp;
        private bool _movingDown;
        private bool _movingLeft;
        private bool _movingRight;
        private bool _run;

        private InputCmdHandler _moveUpCmdHandler;
        private InputCmdHandler _moveDownCmdHandler;
        private InputCmdHandler _moveLeftCmdHandler;
        private InputCmdHandler _moveRightCmdHandler;
        private InputCmdHandler _runCmdHandler;


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
            if (!Owner.HasComponent<IActorComponent>())
                Logger.Error($"[ECS] {Owner.Prototype.Name} - {nameof(PlayerInputMoverComponent)} requires {nameof(IActorComponent)}. ");

            // This component requires that the entity has a PhysicsComponent.
            if (!Owner.HasComponent<PhysicsComponent>())
                Logger.Error($"[ECS] {Owner.Prototype.Name} - {nameof(PlayerInputMoverComponent)} requires {nameof(PhysicsComponent)}. ");

            base.OnAdd();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            IPlayerInput input;
            switch (message)
            {
                case PlayerAttachedMsg msg:
                    InitInputCommands();
                    input = msg.NewPlayer.Input;
                    input.SetCommand(EngineKeyFunctions.MoveUp, _moveUpCmdHandler);
                    input.SetCommand(EngineKeyFunctions.MoveLeft, _moveLeftCmdHandler);
                    input.SetCommand(EngineKeyFunctions.MoveRight, _moveRightCmdHandler);
                    input.SetCommand(EngineKeyFunctions.MoveDown, _moveDownCmdHandler);
                    input.SetCommand(EngineKeyFunctions.Run, _runCmdHandler);
                    break;

                case PlayerDetachedMsg msg:
                    input = msg.OldPlayer.Input;
                    input.SetCommand(EngineKeyFunctions.MoveUp, null);
                    input.SetCommand(EngineKeyFunctions.MoveLeft, null);
                    input.SetCommand(EngineKeyFunctions.MoveRight, null);
                    input.SetCommand(EngineKeyFunctions.MoveDown, null);
                    input.SetCommand(EngineKeyFunctions.Run, null);
                    break;
            }
        }

        public void OnUpdate()
        {
            var physics = Owner.GetComponent<PhysicsComponent>();

            if (_moveDir.LengthSquared < 0.001)
            {
                if (physics.LinearVelocity != Vector2.Zero)
                {
                    physics.LinearVelocity = Vector2.Zero;
                }
                return;
            }
            physics.LinearVelocity = _moveDir * (_run ? FastMoveSpeed : BaseMoveSpeed);
            Owner.Transform.LocalRotation = _moveDir.GetDir().ToAngle();
        }

        private void HandleKeyChange()
        {
            // key directions are in screen coordinates
            // _moveDir is in world coordinates
            // if the camera is moved, this needs to be changed

            var x = 0;
            x -= _movingLeft ? 1 : 0;
            x += _movingRight ? 1 : 0;

            var y = 0;
            y += _movingDown ? 1 : 0;
            y -= _movingUp ? 1 : 0;

            _moveDir = new Vector2(x, y);

            // can't normalize zero length vector
            if (_moveDir.LengthSquared > 1.0e-6)
                _moveDir = _moveDir.Normalized;

            if (_moveDir.LengthSquared > 0.001)
            {
                LastDir = _moveDir.GetDir();
            }
        }

        private void InitInputCommands()
        {
            if (_moveUpCmdHandler != null)
            {
                return;
            }

            _moveUpCmdHandler = InputCmdHandler.FromDelegate(
                session => { _movingUp = true; HandleKeyChange(); },
                session => { _movingUp = false; HandleKeyChange(); });
            _moveLeftCmdHandler = InputCmdHandler.FromDelegate(
                session => { _movingLeft = true; HandleKeyChange(); },
                session => { _movingLeft = false; HandleKeyChange(); });
            _moveRightCmdHandler = InputCmdHandler.FromDelegate(
                session => { _movingRight = true; HandleKeyChange(); },
                session => { _movingRight = false; HandleKeyChange(); });
            _moveDownCmdHandler = InputCmdHandler.FromDelegate(
                session => { _movingDown = true; HandleKeyChange(); },
                session => { _movingDown = false; HandleKeyChange(); });
            _runCmdHandler = InputCmdHandler.FromDelegate(
                session => _run = true,
                session => _run = false);
        }
    }
}
