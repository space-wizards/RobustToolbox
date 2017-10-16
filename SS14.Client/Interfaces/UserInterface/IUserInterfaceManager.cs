using System;
using Lidgren.Network;
using OpenTK;
using SFML.Window;
using SS14.Client.Interfaces.Console;
using SS14.Client.UserInterface.Components;
using SS14.Shared;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IUserInterfaceManager
    {
        IDragDropInfo DragInfo { get; }

        IDebugConsole Console { get; }
        void Initialize();

        void AddComponent(GuiComponent component);
        void RemoveComponent(GuiComponent component);
        void ComponentUpdate(GuiComponentType type, params object[] args);
        void DisposeAllComponents();
        void DisposeAllComponents<T>();
        void ResizeComponents();
        void SetFocus(GuiComponent newFocus);
        void RemoveFocus();

        /// <summary>
        ///     Remove focus, but only if the target is currently focused.
        /// </summary>
        void RemoveFocus(GuiComponent target);

        void Update(FrameEventArgs e);
        void Render(FrameEventArgs e);

        void ToggleMoveMode();

        bool KeyDown(KeyEventArgs e);
        void MouseWheelMove(MouseWheelScrollEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        bool MouseUp(MouseButtonEventArgs e);
        bool MouseDown(MouseButtonEventArgs e);
        void MouseEntered(EventArgs e);
        void MouseLeft(EventArgs e);
        bool TextEntered(TextEventArgs e);

        void HandleNetMessage(NetIncomingMessage msg);
    }
}
