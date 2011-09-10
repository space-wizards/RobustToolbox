using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;

namespace SS3D.Atom.Object.Worktop
{
    public class Worktop : Object
    {
        public Worktop()
            : base()
        {
            SetSpriteName(0, "Worktop_single");
            SetSpriteByIndex(0);
            collidable = true;
            snapTogrid = true;
        }

        public override void HandlePush(Lidgren.Network.NetIncomingMessage message)
        {
        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
        }

        public override void Update(double time)
        {
            base.Update(time);
        }

        public override void Render(float xTopLeft, float yTopLeft)
        {
            base.Render(xTopLeft, yTopLeft);
        }

        public override void UpdatePosition()
        {
            base.UpdatePosition();
        }
    }
}
