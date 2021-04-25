using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            IEnumerable<MonitorReg> AllMonitors { get; }

            // Lifecycle stuff
            bool Init();
            bool TryInitMainWindow(Renderer renderer, [NotNullWhen(false)] out string? error);
            void Shutdown();

            void ProcessEvents();
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
            WindowHandle WindowCreate();

            string KeyGetName(Keyboard.Key key);
            int KeyGetScanCode(Keyboard.Key key);
            string KeyGetNameScanCode(int scanCode);

            string ClipboardGetText();
            void ClipboardSetText(string text);

            void UpdateVSync();
            void UpdateMainWindowMode();

            // OpenGL-related stuff.
            void GLMakeContextCurrent(WindowReg reg);
            void GLInitMainContext(bool gles);
        }
    }
}
