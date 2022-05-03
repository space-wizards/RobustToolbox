using System;
using System.Threading.Tasks;
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
            // Lifecycle stuff
            bool Init();
            void Shutdown();

            // Window loop
            void EnterWindowLoop();
            void TerminateWindowLoop();

            // Event pump
            void ProcessEvents(bool single=false);
            void FlushDispose();

            // Cursor
            ICursor CursorGetStandard(StandardCursorShape shape);
            ICursor CursorCreate(Image<Rgba32> image, Vector2i hotSpot);
            void CursorSet(WindowReg window, ICursor? cursor);

            // Window API.
            (WindowReg?, string? error) WindowCreate(
                GLContextSpec? spec,
                WindowCreateParameters parameters,
                WindowReg? share,
                WindowReg? owner);

            void WindowDestroy(WindowReg reg);
            void WindowSetTitle(WindowReg window, string title);
            void WindowSetMonitor(WindowReg window, IClydeMonitor monitor);
            void WindowSetVisible(WindowReg window, bool visible);
            void WindowRequestAttention(WindowReg window);
            void WindowSwapBuffers(WindowReg window);
            uint? WindowGetX11Id(WindowReg window);
            nint? WindowGetX11Display(WindowReg window);
            nint? WindowGetWin32Window(WindowReg window);

            // Keyboard
            string KeyGetName(Keyboard.Key key);

            // Clipboard
            Task<string> ClipboardGetText(WindowReg mainWindow);
            void ClipboardSetText(WindowReg mainWindow, string text);

            void UpdateMainWindowMode();

            // OpenGL-related stuff.
            // Note: you should probably go through GLContextBase instead, which calls these functions.
            void GLMakeContextCurrent(WindowReg? reg);
            void GLSwapInterval(int interval);
            unsafe void* GLGetProcAddress(string procName);

            // Misc
            void RunOnWindowThread(Action a);
        }
    }
}
