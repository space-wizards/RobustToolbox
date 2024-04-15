using System;

namespace Robust.Client.UserInterface.CustomControls
{
    /// <summary>
    ///     Provides some basic functionality for fullscreen UIs.
    /// </summary>
    public abstract class BaseFullscreen : Control
    {
        public bool IsOpen => Parent != null;

        public event Action? OnClose;

        public event Action? OnOpen;

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public virtual void Close()
        {
            if (Parent == null)
                return;

            Parent.RemoveChild(this);
            OnClose?.Invoke();
        }

        public void Open()
        {
            if (!IsOpen)
                UserInterfaceManager.WindowRoot.AddChild(this);

            OnOpen?.Invoke();
        }
    }
}
