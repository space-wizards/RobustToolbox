using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;
using SS3D_shared.GO;

namespace CGO
{
    //Moves an entity based on key binding input
    public class KeyBindingMoverComponent : GameObjectComponent
    {
        private bool MoveUp = false;
        private bool MoveDown = false;
        private bool MoveLeft = false;
        private bool MoveRight = false;

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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);


            if (MoveUp && !MoveLeft && !MoveRight && !MoveDown) // Move Up
                Owner.MoveUp();
            else if (MoveDown && !MoveLeft && !MoveRight && !MoveUp) // Move Down
                Owner.MoveDown();
            else if (MoveLeft && !MoveRight && !MoveUp && !MoveDown) // Move Left
                Owner.MoveLeft();
            else if (MoveRight && !MoveLeft && !MoveUp && !MoveDown) // Move Right
                Owner.MoveRight();
            else if (MoveUp && MoveRight && !MoveLeft && !MoveDown) // Move Up & Right
                Owner.MoveUpRight();
            else if (MoveUp && MoveLeft && !MoveRight && !MoveDown) // Move Up & Left
                Owner.MoveUpLeft();
            else if (MoveDown && MoveRight && !MoveLeft && !MoveUp) // Move Down & Right
                Owner.MoveDownRight();
            else if (MoveDown && MoveLeft && !MoveRight && !MoveUp) // Move Down & Left
                Owner.MoveDownLeft();
        }
    }
}
