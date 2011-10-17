using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using CGO;

namespace SS3D.Atom.Object.Worktop
{
    public class Worktop : Object
    {
        public Worktop()
            : base()
        {
            //SetSpriteName(0, "worktop_single");
            //SetSpriteByIndex(0);
        }

        public override void Initialize()
        {
            base.Initialize();

            collidable = true;
            snapTogrid = true;

            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("worktop_single");
            c.SetSpriteByKey("worktop_single");
        }

        public override void HandlePush(Lidgren.Network.NetIncomingMessage message)
        {
        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
        }

        public override void Update(float time)
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
