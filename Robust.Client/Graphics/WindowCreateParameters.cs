using System;

namespace Robust.Client.Graphics
{
    public sealed class WindowCreateParameters
    {
        public int Width = 1280;
        public int Height = 720;
        public string Title = "";
        public bool Maximized;
        public bool Visible = true;
        public IClydeMonitor? Monitor;
        public bool Fullscreen;

        /// <summary>
        /// The window that will "own" this window.
        /// Owned windows always appear on top of their owners and have some other misc behavior depending on the OS.
        /// </summary>
        public IClydeWindow? Owner;

        /// <summary>
        /// Controls where a window is initially placed when created.
        /// </summary>
        public WindowStartupLocation StartupLocation;

        /// <summary>
        /// Specifies window styling options for the created window.
        /// </summary>
        public OSWindowStyles Styles;
    }

    /// <summary>
    /// Controls where a window is initially placed when created.
    /// </summary>
    public enum WindowStartupLocation : byte
    {
        /// <summary>
        /// The window position is automatically picked by the windowing system.
        /// </summary>
        Manual,

        /// <summary>
        /// The window is positioned at the center of the <see cref="WindowCreateParameters.Owner"/> window.
        /// </summary>
        CenterOwner,
    }

    /// <summary>
    /// Specifies window styling options for an OS window.
    /// </summary>
    [Flags]
    public enum OSWindowStyles
    {
        /// <summary>
        /// No special styles set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Hide title buttons such as close and minimize.
        /// </summary>
        NoTitleOptions = 1 << 0
    }
}
