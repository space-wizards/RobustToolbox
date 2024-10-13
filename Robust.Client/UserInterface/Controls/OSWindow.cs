using System;
using System.ComponentModel;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using TerraFX.Interop.Windows;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    /// Represents an operating system-based UI window.
    /// </summary>
    /// <seealso cref="BaseWindow"/>
    [Virtual]
    public class OSWindow : Control
    {
        [Dependency] private readonly IClyde _clyde = default!;

        private string _title = "Window";
        private WindowRoot? _root;

        /// <summary>
        /// The window instance backing this window instance.
        /// Not guaranteed to be available unless the window is currently open.
        /// </summary>
        public IClydeWindow? ClydeWindow { get; private set; }

        /// <summary>
        /// The title of the window.
        /// </summary>
        /// <remarks>
        /// Can be changed while the window is open.
        /// </remarks>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                if (ClydeWindow != null)
                    ClydeWindow.Title = _title;
            }
        }

        /// <summary>
        /// Raised when the window is being closed
        /// (via calling <see cref="Close"/> or the user clicking the close button).
        /// Can be cancelled.
        /// </summary>
        public event Action<CancelEventArgs>? Closing;

        /// <summary>
        /// Raised when the window has been closed.
        /// </summary>
        public event Action? Closed;

        public OSWindow() : this(title: null) { }

        /// <param name="title">The title of the window.</param>
        /// <param name="windowStyles">Specifies window styling options for the window.</param>
        /// <param name="owner">
        /// The window that will "own" this window.
        /// Owned windows always appear on top of their owners and have some other misc behavior depending on the OS.
        /// </param>
        /// <param name="startupLocation">The location to place the window at when it is opened.</param>
        public OSWindow(string? title = null, OSWindowStyles windowStyles = OSWindowStyles.None, IClydeWindow? owner = null, WindowStartupLocation startupLocation = WindowStartupLocation.Manual)
        {
            IoCManager.InjectDependencies(this);

            if (title != null)
                Title = title;

            var parameters = new WindowCreateParameters
            {
                Title = Title,
                Styles = windowStyles,
                Owner = owner,
                StartupLocation = startupLocation,
                Visible = false
            };

            ClydeWindow = _clyde.CreateWindow(parameters);
            ClydeWindow.RequestClosed += OnWindowRequestClosed;
            ClydeWindow.Destroyed += OnWindowDestroyed;
            ClydeWindow.Resized += OnWindowResized;

            _root = UserInterfaceManager.CreateWindowRoot(ClydeWindow);
            _root.AddChild(this);
        }

        /// <summary>
        /// Show the window to the user.
        /// </summary>
        public void Show() {
            if (ClydeWindow == null)
                return;

            ClydeWindow.IsVisible = true;
            Shown();
        }

        protected virtual void Shown()
        {

        }

        /// <summary>
        /// Try to close the window.
        /// </summary>
        /// <remarks>
        /// This can be cancelled by anything handling <see cref="Closing"/>.
        /// </remarks>
        public void Close()
        {
            if (ClydeWindow == null)
                return;

            var eventArgs = new CancelEventArgs();
            Closing?.Invoke(eventArgs);
            if (eventArgs.Cancel)
                return;

            ClydeWindow.Dispose();
        }

        public void Resize(Vector2i size)
        {
            if (ClydeWindow == null)
                return;

            ClydeWindow.Size = size;
        }

        /// <summary>
        /// Sizes one or both axis of the window to fit the content.
        /// </summary>
        public void ResizeToContent(WindowSizeToContent axis)
        {
            if (ClydeWindow == null)
                return;

            Measure(Vector2Helpers.Infinity);

            Vector2i size = ClydeWindow.Size;

            if ((axis & WindowSizeToContent.Width) != 0)
                size.X = (int)DesiredSize.X;
            if ((axis & WindowSizeToContent.Height) != 0)
                size.Y = (int)DesiredSize.Y;

            ClydeWindow.Size = size;
        }

        private void OnWindowRequestClosed(WindowRequestClosedEventArgs eventArgs)
        {
            Close();
        }

        private void OnWindowDestroyed(WindowDestroyedEventArgs obj)
        {
            // I give it a 75% chance that some Linux user can force close the window,
            // breaking GLFW,
            // and forcing us to have a code path that ignores the RequestClosed code.

            RealClosed();
        }

        private void OnWindowResized(WindowResizedEventArgs obj)
        {
            SetSize = obj.NewSize;
        }

        private void RealClosed()
        {
            Orphan();

            ClydeWindow = null;
            _root = null;

            Closed?.Invoke();
        }
    }

    [Flags]
    public enum WindowSizeToContent : byte
    {
        Width = 1 << 0,
        Height = 1 << 1,
        WidthAndHeight = Width | Height
    }
}
