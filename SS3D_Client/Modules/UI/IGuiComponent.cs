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

        Point Position
        {
            get;
            set;
        }

        void Render();

        void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message);

        bool MouseDown(MouseInputEventArgs e);
        bool MouseUp(MouseInputEventArgs e);
        void MouseMove(MouseInputEventArgs e);
        bool KeyDown(KeyboardInputEventArgs e);
        
    }
}
