using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using SS3D_shared;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace SS3D.Modules.UI
{
    public interface IGuiComponent
    {
        GuiComponentType componentClass
        {
            get;
        }

        void Render();

        void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message);

        void MouseDown(MouseInputEventArgs e);
        void MouseUp(MouseInputEventArgs e);
    }
}
