using CGO;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using GorgonLibrary;

namespace CGO
{
    //Moves an entity based on key binding input
    public class KeyBindingMoverComponent : GameObjectComponent
    {
        private const float BaseMoveSpeed = 300f;
        private const float FastMoveSpeed = 500f;
        private float _moveTimeCache = 0;
        private const float MoveRateLimit = .06666f; // 15 movements allowed to be sent to the server per second.

        private float _currentMoveSpeed;

        private bool _moveUp;
        private bool _moveDown;
        private bool _moveLeft;
        private bool _moveRight;
        private Vector2D Velocity
        {
            get { return Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity; }
            set { Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity = value; }
        }

        private Direction _movedir;

        public KeyBindingMoverComponent()
        {
            Family = ComponentFamily.Mover; ;
            _currentMoveSpeed = BaseMoveSpeed;
            _movedir = Direction.South;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            /*var x = (float)message.MessageParameters[0];
            var y = (float)message.MessageParameters[1];
            if((bool)message.MessageParameters[2]) //"forced" parameter -- if true forces position update
            PlainTranslate((float)x, (float)y);*/
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;
            switch (type)
            {
                case ComponentMessageType.BoundKeyChange:
                    HandleKeyChange(list);
                    break;
                case ComponentMessageType.GetMoveDir:
                    reply = new ComponentReplyMessage(ComponentMessageType.MoveDirection, _movedir);
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
            var setting = state == BoundKeyState.Down;

            if(state == BoundKeyState.Up)
                SendPositionUpdate(); // Send a position update so that the server knows what position the client ended at.

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
                Velocity = new Vector2D(0, -1) * _currentMoveSpeed;
            }
            else if (_moveDown && !_moveLeft && !_moveRight && !_moveUp) // Move Down
            {
                Velocity = new Vector2D(0, 1) * _currentMoveSpeed;
            }
            else if (_moveLeft && !_moveRight && !_moveUp && !_moveDown) // Move Left
            {
                Velocity = new Vector2D(-1, 0) * _currentMoveSpeed;
            }
            else if (_moveRight && !_moveLeft && !_moveUp && !_moveDown) // Move Right
            {
                Velocity = new Vector2D(1, 0) * _currentMoveSpeed;
            }
            else if (_moveUp && _moveRight && !_moveLeft && !_moveDown) // Move Up & Right
            {
                Velocity = new Vector2D(0.7071f, -0.7071f) * _currentMoveSpeed;
            }
            else if (_moveUp && _moveLeft && !_moveRight && !_moveDown) // Move Up & Left
            {
                Velocity = new Vector2D(-0.7071f, -0.7071f) * _currentMoveSpeed;
            }
            else if (_moveDown && _moveRight && !_moveLeft && !_moveUp) // Move Down & Right
            {
                Velocity = new Vector2D(0.7071f, 0.7071f) * _currentMoveSpeed;
            }
            else if (_moveDown && _moveLeft && !_moveRight && !_moveUp) // Move Down & Left
            {
                Velocity = new Vector2D(-0.7071f, 0.7071f) * _currentMoveSpeed;
            } 
            else
            {
                Velocity = new Vector2D(0f,0f);
            }

            UpdatePosition(frameTime);
        }

        private void UpdatePosition(float frameTime)
        {
            Translate(Velocity * frameTime);
        }

        private void SetMoveDir(Direction movedir)
        {
            if (movedir == _movedir) return;

            _movedir = movedir;
            Owner.SendMessage(this, ComponentMessageType.MoveDirection, _movedir);
        }

        public virtual void SendPositionUpdate()
        {
            Owner.SendComponentNetworkMessage(this, 
                Lidgren.Network.NetDeliveryMethod.ReliableUnordered, 
                Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X, 
                Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y,
                Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity.X,
                Owner.GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity.Y);
        }

        public void PlainTranslate(float x, float y)
        {
            var delta = new Vector2D(x, y) - Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;

            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = new Vector2D(x, y);
            
            if (delta.X > 0 && delta.Y > 0)
                SetMoveDir(Direction.SouthEast);
            if (delta.X > 0 && delta.Y < 0)
                SetMoveDir(Direction.NorthEast);
            if (delta.X < 0 && delta.Y > 0)
                SetMoveDir(Direction.SouthWest);
            if (delta.X < 0 && delta.Y < 0)
                SetMoveDir(Direction.NorthWest);
            if (delta.X > 0 && delta.Y == 0)
                SetMoveDir(Direction.East);
            if (delta.X < 0 && delta.Y == 0)
                SetMoveDir(Direction.West);
            if (delta.Y > 0 && delta.X == 0)
                SetMoveDir(Direction.South);
            if (delta.Y < 0 && delta.X == 0)
                SetMoveDir(Direction.North);

            //Owner.Moved();
        }

        /// <summary>
        /// Moves the entity and sends an update packet to the serverside mover component.
        /// </summary>
        /// <param name="translationVector"></param>
        public virtual void Translate(Vector2D translationVector)
        {
            var oldPos = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;

            var translated = TryTranslate(translationVector, false); //Only bump once...
            if (!translated)
                translated = TryTranslate(new Vector2D(translationVector.X, 0), true);
            if (!translated)
                translated = TryTranslate(new Vector2D(0, translationVector.Y), true);
            if (translated)
            {
                var delta = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position - oldPos;
                if (delta.X > 0 && delta.Y > 0)
                    SetMoveDir(Direction.SouthEast);
                if (delta.X > 0 && delta.Y < 0)
                    SetMoveDir(Direction.NorthEast);
                if (delta.X < 0 && delta.Y > 0)
                    SetMoveDir(Direction.SouthWest);
                if (delta.X < 0 && delta.Y < 0)
                    SetMoveDir(Direction.NorthWest);
                if (delta.X > 0 && delta.Y == 0)
                    SetMoveDir(Direction.East);
                if (delta.X < 0 && delta.Y == 0)
                    SetMoveDir(Direction.West);
                if (delta.Y > 0 && delta.X == 0)
                    SetMoveDir(Direction.South);
                if (delta.Y < 0 && delta.X == 0)
                    SetMoveDir(Direction.North);


                if (_moveTimeCache >= MoveRateLimit)
                {
                    SendPositionUpdate();

                    _moveTimeCache = 0;
                }
            }
            //Owner.Moved();
        }

        /// <summary>
        /// Tries to move the entity. Checks collision. If the entity _is_ colliding, move it back and return false.
        /// </summary>
        /// <param name="translationVector"></param>
        /// <param name="suppressBump"></param>
        /// <returns></returns>
        public bool TryTranslate(Vector2D translationVector, bool suppressBump)
        {
            var oldPosition = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position += translationVector; // We move the sprite here rather than the position, as we can then use its updated AABB values.
            //Check collision.
            var reply = Owner.SendMessage(this, ComponentFamily.Collider, ComponentMessageType.CheckCollision, false);
            if (reply.MessageType == ComponentMessageType.CollisionStatus)
            {
                var colliding = (bool)reply.ParamsList[0];
                if (colliding) //Collided, reset position and return false.
                {
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = oldPosition;
                    return false;
                }
            }
            return true;
        }
    }
}
