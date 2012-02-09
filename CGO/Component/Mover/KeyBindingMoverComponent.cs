using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;
using GorgonLibrary;

namespace CGO
{
    //Moves an entity based on key binding input
    public class KeyBindingMoverComponent : GameObjectComponent
    {
        private bool MoveUp = false;
        private bool MoveDown = false;
        private bool MoveLeft = false;
        private bool MoveRight = false;
        private float baseMoveSpeed = 300f;
        private float fastMoveSpeed = 500f;
        private float currentMoveSpeed;

        private Constants.MoveDirs movedir = Constants.MoveDirs.south;

        public KeyBindingMoverComponent()
        {
            family = ComponentFamily.Mover;
            currentMoveSpeed = baseMoveSpeed;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            double x = (double)message.MessageParameters[0];
            double y = (double)message.MessageParameters[1];
            if((bool)message.MessageParameters[2]) //"forced" parameter -- if true forces position update
            plainTranslate((float)x, (float)y);
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);

            if (sender == this)
                return;
            switch (type)
            {
                case ComponentMessageType.BoundKeyChange:
                    HandleKeyChange(list);
                    break;
                case ComponentMessageType.GetMoveDir:
                    replies.Add(new ComponentReplyMessage(ComponentMessageType.MoveDirection, movedir));
                    break;
                default:
                    break;
            }
            return;
        }

        /// <summary>
        /// Handles a changed keystate message
        /// </summary>
        /// <param name="list">0 - Function, 1 - Key State</param>
        private void HandleKeyChange(params object[] list)
        {
            BoundKeyFunctions function = (BoundKeyFunctions)list[0];
            BoundKeyState state = (BoundKeyState)list[1];
            bool setting = false;
            if (state == BoundKeyState.Down)
                setting = true;
            if (state == BoundKeyState.Up)
                setting = false;

            if (function == BoundKeyFunctions.MoveDown)
                MoveDown = setting;
            if (function == BoundKeyFunctions.MoveUp)
                MoveUp = setting;
            if (function == BoundKeyFunctions.MoveLeft)
                MoveLeft = setting;
            if (function == BoundKeyFunctions.MoveRight)
                MoveRight = setting;
            if (function == BoundKeyFunctions.Run)
            {
                if (setting)
                    currentMoveSpeed = fastMoveSpeed;
                else
                    currentMoveSpeed = baseMoveSpeed;
            }
        }

        /// <summary>
        /// Update function. Processes currently pressed keys and does shit etc.
        /// </summary>
        /// <param name="frameTime"></param>
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            
            if (MoveUp && !MoveLeft && !MoveRight && !MoveDown) // Move Up
            {
                Translate(new Vector2D(0, -1) * currentMoveSpeed * frameTime);
            }
            else if (MoveDown && !MoveLeft && !MoveRight && !MoveUp) // Move Down
            {
                Translate(new Vector2D(0, 1) * currentMoveSpeed * frameTime);
            }
            else if (MoveLeft && !MoveRight && !MoveUp && !MoveDown) // Move Left
            {
                Translate(new Vector2D(-1, 0) * currentMoveSpeed * frameTime);
            }
            else if (MoveRight && !MoveLeft && !MoveUp && !MoveDown) // Move Right
            {
                Translate(new Vector2D(1, 0) * currentMoveSpeed * frameTime);
            }
            else if (MoveUp && MoveRight && !MoveLeft && !MoveDown) // Move Up & Right
            {
                Translate(new Vector2D(0.7071f, -0.7071f) * currentMoveSpeed * frameTime);
            }
            else if (MoveUp && MoveLeft && !MoveRight && !MoveDown) // Move Up & Left
            {
                Translate(new Vector2D(-0.7071f, -0.7071f) * currentMoveSpeed * frameTime);
            }
            else if (MoveDown && MoveRight && !MoveLeft && !MoveUp) // Move Down & Right
            {
                Translate(new Vector2D(0.7071f, 0.7071f) * currentMoveSpeed * frameTime);
            }
            else if (MoveDown && MoveLeft && !MoveRight && !MoveUp) // Move Down & Left
            {
                Translate(new Vector2D(-0.7071f, 0.7071f) * currentMoveSpeed * frameTime);
            }
        }

        private void SetMoveDir(Constants.MoveDirs _movedir)
        {
            if (_movedir != movedir)
            {
                movedir = _movedir;
                Owner.SendMessage(this, ComponentMessageType.MoveDirection, null, movedir);
            }
        }

        public virtual void SendPositionUpdate()
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, Owner.Position.X, Owner.Position.Y);
        }

        public void plainTranslate(float x, float y)
        {
            Vector2D delta = new Vector2D(x, y) - Owner.Position;

            Owner.Position = new Vector2D(x, y);
            
            if (delta.X > 0 && delta.Y > 0)
                SetMoveDir(Constants.MoveDirs.southeast);
            if (delta.X > 0 && delta.Y < 0)
                SetMoveDir(Constants.MoveDirs.northeast);
            if (delta.X < 0 && delta.Y > 0)
                SetMoveDir(Constants.MoveDirs.southwest);
            if (delta.X < 0 && delta.Y < 0)
                SetMoveDir(Constants.MoveDirs.northwest);
            if (delta.X > 0 && delta.Y == 0)
                SetMoveDir(Constants.MoveDirs.east);
            if (delta.X < 0 && delta.Y == 0)
                SetMoveDir(Constants.MoveDirs.west);
            if (delta.Y > 0 && delta.X == 0)
                SetMoveDir(Constants.MoveDirs.south);
            if (delta.Y < 0 && delta.X == 0)
                SetMoveDir(Constants.MoveDirs.north);

            Owner.Moved();
        }

        /// <summary>
        /// Moves the entity and sends an update packet to the serverside mover component.
        /// </summary>
        /// <param name="translationVector"></param>
        public virtual void Translate(Vector2D translationVector)
        {
            Vector2D oldPos = Owner.Position;
            bool translated = false;
            translated = TryTranslate(translationVector, false); //Only bump once...
            if (!translated)
                translated = TryTranslate(new Vector2D(translationVector.X, 0), true);
            if (!translated)
                translated = TryTranslate(new Vector2D(0, translationVector.Y), true);
            if (translated)
            {
                Vector2D delta = Owner.Position - oldPos;
                if (delta.X > 0 && delta.Y > 0)
                    SetMoveDir(Constants.MoveDirs.southeast);
                if (delta.X > 0 && delta.Y < 0)
                    SetMoveDir(Constants.MoveDirs.northeast);
                if (delta.X < 0 && delta.Y > 0)
                    SetMoveDir(Constants.MoveDirs.southwest);
                if (delta.X < 0 && delta.Y < 0)
                    SetMoveDir(Constants.MoveDirs.northwest);
                if (delta.X > 0 && delta.Y == 0)
                    SetMoveDir(Constants.MoveDirs.east);
                if (delta.X < 0 && delta.Y == 0)
                    SetMoveDir(Constants.MoveDirs.west);
                if (delta.Y > 0 && delta.X == 0)
                    SetMoveDir(Constants.MoveDirs.south);
                if (delta.Y < 0 && delta.X == 0)
                    SetMoveDir(Constants.MoveDirs.north);

                SendPositionUpdate();
            }
            Owner.Moved();
        }

        /// <summary>
        /// Tries to move the entity. Checks collision. If the entity _is_ colliding, move it back and return false.
        /// </summary>
        /// <param name="translationVector"></param>
        /// <param name="SuppressBump"></param>
        /// <returns></returns>
        public bool TryTranslate(Vector2D translationVector, bool SuppressBump)
        {
            Vector2D oldPosition = Owner.Position;
            Owner.Position += translationVector; // We move the sprite here rather than the position, as we can then use its updated AABB values.
            //Check collision.
            var replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.CheckCollision, replies, false);
            if (replies.Count > 0 && replies.First().MessageType == ComponentMessageType.CollisionStatus)
            {
                bool colliding = (bool)replies.First().ParamsList[0];
                if (colliding) //Collided, reset position and return false.
                {
                    Owner.Position = oldPosition;
                    return false;
                }
            }
            return true;
        }
    }
}
