using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using OpenToolkit;
using Robust.Client.Input;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics.Clyde
{
    partial class Clyde
    {
        private interface IWindowingImpl
        {
            WindowReg? MainWindow { get; }
            IReadOnlyList<WindowReg> AllWindows { get; }
            IBindingsContext GraphicsBindingContext { get; }

            // Lifecycle stuff
            bool Init();
            bool TryInitMainWindow(Renderer renderer, [NotNullWhen(false)] out string? error);
            void Shutdown();

            void EnterWindowLoop();
            void TerminateWindowLoop();

            void ProcessEvents(bool single=false);
            void FlushDispose();

            ICursor CursorGetStandard(StandardCursorShape shape);
            ICursor CursorCreate(Image<Rgba32> image, Vector2i hotSpot);
            void CursorSet(WindowReg window, ICursor? cursor);

            void WindowSetTitle(WindowReg window, string title);
            void WindowSetMonitor(WindowReg window, IClydeMonitor monitor);
            void WindowSetVisible(WindowReg window, bool visible);
            void WindowRequestAttention(WindowReg window);
            void WindowSwapBuffers(WindowReg window);
            uint? WindowGetX11Id(WindowReg window);
            nint? WindowGetWin32Window(WindowReg window);
            Task<WindowHandle> WindowCreate(WindowCreateParameters parameters);
            void WindowDestroy(WindowReg reg);

            string KeyGetName(Keyboard.Key key);

            Task<string> ClipboardGetText();
            void ClipboardSetText(string text);

            void UpdateVSync();
            void UpdateMainWindowMode();

            // OpenGL-related stuff.
            void GLMakeContextCurrent(WindowReg reg);
            void GLSwapInterval(int interval);
            void GLInitMainContext(bool gles);
        }
    }
}
