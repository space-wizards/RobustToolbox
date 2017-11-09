using System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Console;
using SS14.Client.UserInterface.Controls;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IUserInterfaceManager
    {
        IDragDropInfo DragInfo { get; }

        IDebugConsole Console { get; }
        void Initialize();

        void AddComponent(Control component);
        void RemoveComponent(Control component);
        void DisposeAllComponents();
        void DisposeAllComponents<T>();
        void ResizeComponents();
        IEnumerable<T> GetAllComponents<T>() where T: Control;
        IEnumerable<Control> GetAllComponents(Type type);
        T GetSingleComponents<T>() where T : Control;
        Control GetSingleComponent(Type type);

        void SetFocus(Control newFocus);
        void RemoveFocus();
        bool HasFocus(Control control);

        /// <summary>
        ///     Remove focus, but only if the target is currently focused.
        /// </summary>
        void RemoveFocus(Control target);

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
    }
}
