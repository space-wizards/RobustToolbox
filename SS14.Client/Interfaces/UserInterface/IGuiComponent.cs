using Lidgren.Network;
using SS14.Client.Graphics.Input;
using SS14.Shared;
using SS14.Shared.Maths;
using System;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IGuiComponent : IDisposable
    {
        /// <summary>
        ///  <para>Defines the type of UI component.</para>
        ///  <para>This needs to be set if you want the component to receive network input as it is used for routing the messages to the correct components.</para>
        /// </summary>
        GuiComponentType ComponentClass { get; }

        Vector2i Position { get; set; }

        Box2i ClientArea { get; set; }
        bool ReceiveInput { get; set; }

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
        bool MouseWheelMove(MouseWheelScrollEventArgs e);
        bool KeyDown(KeyEventArgs e);
        bool TextEntered(TextEventArgs e);

        void ComponentUpdate(params object[] args);
    }
}
