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
        public bool HideCloseButton;
        public IClydeWindow? Owner;

        /// <summary>
        /// Controls where a window is initially placed when created.
        /// </summary>
        public WindowStartupLocation StartupLocation;
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
}
