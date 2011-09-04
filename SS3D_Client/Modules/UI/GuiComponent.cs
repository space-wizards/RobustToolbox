using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SS3D_shared;

using GorgonLibrary;
using GorgonLibrary.InputDevices;

namespace SS3D.Modules.UI
{
    public class GuiComponent : IGuiComponent
    {
        public GuiComponentType componentClass
        {
            get;
            protected set;
        }
        protected PlayerController playerController;
        protected Point position;
        public virtual Point Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }


        public GuiComponent(PlayerController _playerController)
        {
            playerController = _playerController;
        }

        public virtual void Render()
        {
        }

        public virtual void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message)
        {
        }

        public virtual bool MouseDown(MouseInputEventArgs e)
        {
            return false;
        }

        public virtual bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }

        public virtual void MouseMove(MouseInputEventArgs e)
        {
        }

        public virtual bool KeyDown(KeyboardInputEventArgs e)
        {
            return false;
        }
    }
}
