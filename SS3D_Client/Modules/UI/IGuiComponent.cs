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
    public interface IGuiComponent : IDisposable
    {
        /// <summary>
        ///  <para>Defines the type of UI component.</para>
        ///  <para>This needs to be set if you want the component to recieve network input as it is used for routing the messages to the correct components.</para>
        /// </summary>
        GuiComponentType componentClass
        {
            get;
        }

        Point Position
        {
            get;
            set;
        }

        void Update();
        void Render();

        void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message);

        void ToggleVisible();
        void SetVisible(bool vis);
        bool IsVisible();

        bool MouseDown(MouseInputEventArgs e);
        bool MouseUp(MouseInputEventArgs e);
        void MouseMove(MouseInputEventArgs e);
        bool KeyDown(KeyboardInputEventArgs e);

        bool RecieveInput
        {
            get;
            set;
        }

        int zDepth
        {
            get;
            set;
        }
  
    }
}
