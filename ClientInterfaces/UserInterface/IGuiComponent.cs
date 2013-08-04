using System;
using System.Drawing;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;

namespace ClientInterfaces.UserInterface
{
    public interface IGuiComponent : IDisposable
    {
        /// <summary>
        ///  <para>Defines the type of UI component.</para>
        ///  <para>This needs to be set if you want the component to recieve network input as it is used for routing the messages to the correct components.</para>
        /// </summary>
        GuiComponentType ComponentClass { get; }

        Point Position { get; set; }

        Rectangle ClientArea { get; set; }
        bool RecieveInput { get; set; }

        bool Focus { get; set; }

        int ZDepth { get; set; }

        void Update(float frameTime);
        void Render();
        void Resize();

        void HandleNetworkMessage(NetIncomingMessage message);

        void ToggleVisible();
        void SetVisible(bool vis);
        bool IsVisible();

        bool MouseDown(MouseInputEventArgs e);
        bool MouseUp(MouseInputEventArgs e);
        void MouseMove(MouseInputEventArgs e);
        bool MouseWheelMove(MouseInputEventArgs e);
        bool KeyDown(KeyboardInputEventArgs e);

        void ComponentUpdate(params object[] args);
    }
}