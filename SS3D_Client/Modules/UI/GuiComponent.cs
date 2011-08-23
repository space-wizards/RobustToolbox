using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private PlayerController playerController;


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

        public virtual void MouseDown(MouseInputEventArgs e)
        {

        }

        public virtual void MouseUp(MouseInputEventArgs e)
        {

        }
    }
}
