using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using CGO;

namespace SS3D.Atom.Mob.HelperClasses
{
    public class Appendage
    {
        public string bone;
        public string appendageName;
        public Mob owner;
        public Entity attachedItem;
        public int ID;
        public Vector2D holdPosition;

        public Appendage(string _bone, string _appendageName, int _ID, Mob _owner)
        {
            bone = _bone;
            appendageName = _appendageName;
            owner = _owner;
            ID = _ID;
            SetHoldPosition();
        }

        public void SetHoldPosition()
        {
            switch (appendageName)
            {
                case "LeftHand":
                    holdPosition = new Vector2D(11, 6);
                    break;
                case "RightHand":
                    holdPosition = new Vector2D(-11, 6);
                    break;
                default:
                    holdPosition = Vector2D.Zero;
                    break;
            }
        }

        public Vector2D GetHoldPosition(int holderIndex)
        {
            Vector2D holdPos = Vector2D.Zero;

            switch (holderIndex)
            {
                case 0: // North
                    holdPos = new Vector2D(holdPosition.X * -1, holdPosition.Y);
                    break;
                case 2: // South
                    holdPos = holdPosition;
                    break;
                default:
                    break;
            }

            return holdPos;

        }
    }
}
