using System;
using System.ComponentModel;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    /// Represents an operating system-based UI window.
    /// </summary>
    /// <seealso cref="BaseWindow"/>
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
        /// The window that will "own" this window.
        /// Owned windows always appear on top of their owners and have some other misc behavior depending on the OS.
        /// </summary>
        public IClydeWindow? Owner { get; set; }

        /// <summary>
        /// Whether the window is currently open.
        /// </summary>
        public bool IsOpen => ClydeWindow != null;

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
                    ClydeWindow.Title = value;
            }
        }

        /// <summary>
        /// The location to place the window at when it is opened.
        /// </summary>
        /// <remarks>
        /// Changing this while the window is open has no effect.
        /// </remarks>
        public WindowStartupLocation StartupLocation { get; set; }

        /// <summary>
        /// Controls whether to automatically size one or both axis of the window to fit the content.
        /// </summary>
        /// <remarks>
        /// Changing this while the window is open has no effect.
        /// </remarks>
        public WindowSizeToContent SizeToContent { get; set; }

        /// <summary>
        /// Specifies window styling options for the window.
        /// </summary>
        /// <remarks>
        /// Changing this while the window is open has no effect.
        /// </remarks>
        public OSWindowStyles WindowStyles { get; set; }

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

        public OSWindow()
        {
            IoCManager.InjectDependencies(this);
        }

        /// <summary>
        /// Show the window to the user.
        /// </summary>
        public void Show()
        {
            if (IsOpen)
                return;

            var parameters = new WindowCreateParameters();

            if (!float.IsNaN(SetWidth))
                parameters.Width = (int) SetWidth;

            if (!float.IsNaN(SetHeight))
                parameters.Height = (int) SetHeight;

            if (SizeToContent != WindowSizeToContent.Manual)
            {
                Measure(Vector2.Infinity);

                if ((SizeToContent & WindowSizeToContent.Width) != 0)
                    parameters.Width = (int)DesiredSize.X;

                if ((SizeToContent & WindowSizeToContent.Height) != 0)
                    parameters.Height = (int)DesiredSize.Y;
            }

            parameters.Title = _title;
            parameters.Styles = WindowStyles;
            parameters.Owner = Owner;
            parameters.StartupLocation = StartupLocation;

            ClydeWindow = _clyde.CreateWindow(parameters);
            ClydeWindow.RequestClosed += OnWindowRequestClosed;
            ClydeWindow.Destroyed += OnWindowDestroyed;

            _root = UserInterfaceManager.CreateWindowRoot(ClydeWindow);
            _root.AddChild(this);
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
        Manual = 0,
        Width = 1 << 0,
        Height = 1 << 1,
        WidthAndHeight = Width | Height
    }
}
