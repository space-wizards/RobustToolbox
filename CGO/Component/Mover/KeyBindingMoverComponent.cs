using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;
using SS3D_shared.GO;
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

        public KeyBindingMoverComponent()
        {
            family = ComponentFamily.Mover;
        }

        public override void RecieveMessage(object sender, MessageType type, params object[] list)
        {
            if (sender == this)
                return;
            switch (type)
            {
                case MessageType.BoundKeyChange:
                    HandleKeyChange(list);
                    break;
                default:
                    break;
            }
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
                Translate(new Vector2D(0,-1) * Owner.speed * frameTime);
                //Owner.MoveUp();
            }
            else if (MoveDown && !MoveLeft && !MoveRight && !MoveUp) // Move Down
            {
                Translate(new Vector2D(0, 1) * Owner.speed * frameTime);
                //Owner.MoveDown();
            }
            else if (MoveLeft && !MoveRight && !MoveUp && !MoveDown) // Move Left
            {
                Translate(new Vector2D(-1, 0) * Owner.speed * frameTime);
                //Owner.MoveLeft(); 
            }
            else if (MoveRight && !MoveLeft && !MoveUp && !MoveDown) // Move Right
            {
                Translate(new Vector2D(1, 0) * Owner.speed * frameTime);
                //Owner.MoveRight(); 
            }
            else if (MoveUp && MoveRight && !MoveLeft && !MoveDown) // Move Up & Right
            {
                Translate(new Vector2D(1, -1) * Owner.speed * frameTime);
                //Owner.MoveUpRight(); 
            }
            else if (MoveUp && MoveLeft && !MoveRight && !MoveDown) // Move Up & Left
            {
                Translate(new Vector2D(-1, -1) * Owner.speed * frameTime);
                //Owner.MoveUpLeft(); 
            }
            else if (MoveDown && MoveRight && !MoveLeft && !MoveUp) // Move Down & Right
            {
                Translate(new Vector2D(1, 1) * Owner.speed * frameTime);
                //Owner.MoveDownRight(); 
            }
            else if (MoveDown && MoveLeft && !MoveRight && !MoveUp) // Move Down & Left
            {
                Translate(new Vector2D(-1, 1) * Owner.speed * frameTime);
                //Owner.MoveDownLeft(); 
            }
        }

        public virtual void SendPositionUpdate()
        {
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableUnordered, Owner.position.X, Owner.position.Y);
        }

        /// <summary>
        /// Moves the entity and sends an update packet to the serverside mover component.
        /// </summary>
        /// <param name="translationVector"></param>
        public virtual void Translate(Vector2D translationVector)
        {
            Vector2D oldPosition = Owner.position;
            Owner.position += translationVector; // We move the sprite here rather than the position, as we can then use its updated AABB values.
            SendPositionUpdate();
        }
    }
}
