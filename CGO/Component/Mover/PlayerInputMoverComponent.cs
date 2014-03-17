using GameObject;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    //Moves an entity based on key binding input
    public class PlayerInputMoverComponent : Component
    {
        private const float BaseMoveSpeed = Constants.HumanWalkSpeed;
        public const float FastMoveSpeed = Constants.HumanRunSpeed;
        private const float MoveRateLimit = .06666f; // 15 movements allowed to be sent to the server per second.

        private float _currentMoveSpeed;

        private bool _moveDown;
        private bool _moveLeft;
        private bool _moveRight;
        private float _moveTimeCache;
        private bool _moveUp;
        public bool ShouldSendPositionUpdate;

        private Direction _movedir;

        public PlayerInputMoverComponent()
        {
            Family = ComponentFamily.Mover;
            ;
            _currentMoveSpeed = BaseMoveSpeed;
            _movedir = Direction.South;
        }

        private Vector2D Velocity
        {
            get { return Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity; }
            set { Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity = value; }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            /*var x = (float)message.MessageParameters[0];
            var y = (float)message.MessageParameters[1];
            if((bool)message.MessageParameters[2]) //"forced" parameter -- if true forces position update
            PlainTranslate((float)x, (float)y);*/
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

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
            var function = (BoundKeyFunctions) list[0];
            var state = (BoundKeyState) list[1];
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
                Velocity = new Vector2D(0, -1)*_currentMoveSpeed;
            }
            else if (_moveDown && !_moveLeft && !_moveRight && !_moveUp) // Move Down
            {
                Velocity = new Vector2D(0, 1)*_currentMoveSpeed;
            }
            else if (_moveLeft && !_moveRight && !_moveUp && !_moveDown) // Move Left
            {
                Velocity = new Vector2D(-1, 0)*_currentMoveSpeed;
            }
            else if (_moveRight && !_moveLeft && !_moveUp && !_moveDown) // Move Right
            {
                Velocity = new Vector2D(1, 0)*_currentMoveSpeed;
            }
            else if (_moveUp && _moveRight && !_moveLeft && !_moveDown) // Move Up & Right
            {
                Velocity = new Vector2D(0.7071f, -0.7071f)*_currentMoveSpeed;
            }
            else if (_moveUp && _moveLeft && !_moveRight && !_moveDown) // Move Up & Left
            {
                Velocity = new Vector2D(-0.7071f, -0.7071f)*_currentMoveSpeed;
            }
            else if (_moveDown && _moveRight && !_moveLeft && !_moveUp) // Move Down & Right
            {
                Velocity = new Vector2D(0.7071f, 0.7071f)*_currentMoveSpeed;
            }
            else if (_moveDown && _moveLeft && !_moveRight && !_moveUp) // Move Down & Left
            {
                Velocity = new Vector2D(-0.7071f, 0.7071f)*_currentMoveSpeed;
            }
            else
            {
                Velocity = new Vector2D(0f, 0f);
            }

            /*Vector2D translationVector = Velocity*frameTime;
            var velcomp = Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity);

            bool translated = TryTranslate(translationVector, false); //Only bump once...
            bool translatedx = false, translatedy = false;
            if (!translated)
                translatedx = TryTranslate(new Vector2D(translationVector.X, 0), true);
            if (!translated && !translatedx)
                translatedy = TryTranslate(new Vector2D(0, translationVector.Y), true);

            if (!translated)
            {
                if (!translatedx)
                {
                    velcomp.Velocity = new Vector2D(0, velcomp.Velocity.Y);
                }
                if (!translatedy)
                    velcomp.Velocity = new Vector2D(velcomp.Velocity.X, 0);
                if (!translatedx && !translatedy)
                    velcomp.Velocity = Vector2D.Zero;

                translationVector = new Vector2D(translatedx?translationVector.X:0, translatedy?translationVector.Y:0);
            }

            if (_moveTimeCache >= MoveRateLimit)
            {
                var nextPosition = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position + translationVector;

                SendPositionUpdate(nextPosition);

                _moveTimeCache = 0;
            }*/
        }

        public virtual void SendPositionUpdate(Vector2D nextPosition)
        {
            Owner.SendComponentNetworkMessage(this,
                                              NetDeliveryMethod.ReliableUnordered,
                                              nextPosition.X,
                                              nextPosition.Y,
                                              Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity.X,
                                              Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity.Y);
        }
    }
}