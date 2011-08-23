using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using SS3D_shared;

using GorgonLibrary;
using GorgonLibrary.Graphics;

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
    }
}
