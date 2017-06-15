using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Shared;
using System;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IGuiComponent : IDisposable
    {
        /// <summary>
        ///  <para>Defines the type of UI component.</para>
        ///  <para>This needs to be set if you want the component to recieve network input as it is used for routing the messages to the correct components.</para>
        /// </summary>
        GuiComponentType ComponentClass { get; }

        Vector2i Position { get; set; }

        IntRect ClientArea { get; set; }
        bool RecieveInput { get; set; }

        bool Focus { get; set; }

        int ZDepth { get; set; }

        string name { get; }

        void Update(float frameTime);
        void Render();
        void Resize();

        void HandleNetworkMessage(NetIncomingMessage message);

        void ToggleVisible();
        void SetVisible(bool vis);
        bool IsVisible();

        bool MouseDown(MouseButtonEventArgs e);
        bool MouseUp(MouseButtonEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        bool MouseWheelMove(MouseWheelEventArgs e);
        bool KeyDown(KeyEventArgs e);
        bool TextEntered(TextEventArgs e);

        void ComponentUpdate(params object[] args);
    }
}
