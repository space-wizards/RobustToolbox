using Lidgren.Network;
using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects
{
    //Moves an entity based on key binding input
    public class PlayerInputMoverComponent : Component, IMoverComponent
    {
        public override string Name => "PlayerInputMover";
        public override uint? NetID => NetIDs.PLAYER_INPUT_MOVER;
        public override bool NetworkSynchronizeExistence => true;

        private const float BaseMoveSpeed = Constants.HumanWalkSpeed;
        public const float FastMoveSpeed = Constants.HumanRunSpeed;
        private const float MoveRateLimit = .06666f; // 15 movements allowed to be sent to the server per second.

        private float _currentMoveSpeed = BaseMoveSpeed;

        private bool _moveDown;
        private bool _moveLeft;
        private bool _moveRight;
        private float _moveTimeCache;
        private bool _moveUp;
        public bool ShouldSendPositionUpdate;

        private Vector2f Velocity
        {
            get => Owner.GetComponent<IVelocityComponent>().Velocity;
            set => Owner.GetComponent<IVelocityComponent>().Velocity = value;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            /*var x = (float)message.MessageParameters[0];
            var y = (float)message.MessageParameters[1];
            if((bool)message.MessageParameters[2]) //"forced" parameter -- if true forces position update
            PlainTranslate((float)x, (float)y);*/
        }

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;
            switch (type)
            {
                case ComponentMessageType.BoundKeyChange:
                    HandleKeyChange(list);
                    break;
            }
            return reply;
        }

        /// <summary>
        /// Handles a changed keystate message
        /// </summary>
        /// <param name="list">0 - Function, 1 - Key State</param>
        private void HandleKeyChange(params object[] list)
        {
            var function = (BoundKeyFunctions)list[0];
            var state = (BoundKeyState)list[1];
            bool setting = state == BoundKeyState.Down;

            ShouldSendPositionUpdate = true;
            /*if (state == BoundKeyState.Up)
                SendPositionUpdate(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);*/
            // Send a position update so that the server knows what position the client ended at.

            if (function == BoundKeyFunctions.MoveDown)
                _moveDown = setting;
            if (function == BoundKeyFunctions.MoveUp)
                _moveUp = setting;
            if (function == BoundKeyFunctions.MoveLeft)
                _moveLeft = setting;
            if (function == BoundKeyFunctions.MoveRight)
                _moveRight = setting;
            if (function == BoundKeyFunctions.Run)
            {
                _currentMoveSpeed = setting ? FastMoveSpeed : BaseMoveSpeed;
            }
        }

        /// <summary>
        /// Update function. Processes currently pressed keys and does shit etc.
        /// </summary>
        /// <param name="frameTime"></param>
        public override void Update(float frameTime)
        {
            _moveTimeCache += frameTime;

            base.Update(frameTime);

            if (_moveUp && !_moveLeft && !_moveRight && !_moveDown) // Move Up
            {
                Velocity = new Vector2f(0, -1) * _currentMoveSpeed;
            }
            else if (_moveDown && !_moveLeft && !_moveRight && !_moveUp) // Move Down
            {
                Velocity = new Vector2f(0, 1) * _currentMoveSpeed;
            }
            else if (_moveLeft && !_moveRight && !_moveUp && !_moveDown) // Move Left
            {
                Velocity = new Vector2f(-1, 0) * _currentMoveSpeed;
            }
            else if (_moveRight && !_moveLeft && !_moveUp && !_moveDown) // Move Right
            {
                Velocity = new Vector2f(1, 0) * _currentMoveSpeed;
            }
            else if (_moveUp && _moveRight && !_moveLeft && !_moveDown) // Move Up & Right
            {
                Velocity = new Vector2f(0.7071f, -0.7071f) * _currentMoveSpeed;
            }
            else if (_moveUp && _moveLeft && !_moveRight && !_moveDown) // Move Up & Left
            {
                Velocity = new Vector2f(-0.7071f, -0.7071f) * _currentMoveSpeed;
            }
            else if (_moveDown && _moveRight && !_moveLeft && !_moveUp) // Move Down & Right
            {
                Velocity = new Vector2f(0.7071f, 0.7071f) * _currentMoveSpeed;
            }
            else if (_moveDown && _moveLeft && !_moveRight && !_moveUp) // Move Down & Left
            {
                Velocity = new Vector2f(-0.7071f, 0.7071f) * _currentMoveSpeed;
            }
            else
            {
                Velocity = new Vector2f(0f, 0f);
            }

            /*Vector2 translationVector = Velocity*frameTime;
            var velcomp = Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity);

            bool translated = TryTranslate(translationVector, false); //Only bump once...
            bool translatedx = false, translatedy = false;
            if (!translated)
                translatedx = TryTranslate(new Vector2(translationVector.X, 0), true);
            if (!translated && !translatedx)
                translatedy = TryTranslate(new Vector2(0, translationVector.Y), true);

            if (!translated)
            {
                if (!translatedx)
                {
                    velcomp.Velocity = new Vector2(0, velcomp.Velocity.Y);
                }
                if (!translatedy)
                    velcomp.Velocity = new Vector2(velcomp.Velocity.X, 0);
                if (!translatedx && !translatedy)
                    velcomp.Velocity = Vector2.Zero;

                translationVector = new Vector2(translatedx?translationVector.X:0, translatedy?translationVector.Y:0);
            }

            if (_moveTimeCache >= MoveRateLimit)
            {
                var nextPosition = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position + translationVector;

                SendPositionUpdate(nextPosition);

                _moveTimeCache = 0;
            }*/
        }

        public virtual void SendPositionUpdate(Vector2f nextPosition)
        {
            var velocity = Owner.GetComponent<IVelocityComponent>();
            Owner.SendComponentNetworkMessage(this,
                                              NetDeliveryMethod.ReliableUnordered,
                                              nextPosition.X,
                                              nextPosition.Y,
                                              velocity.Velocity.X,
                                              velocity.Velocity.Y);
        }
    }
}
