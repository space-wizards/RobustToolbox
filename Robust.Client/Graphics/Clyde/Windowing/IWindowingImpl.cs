using System;
using System.Threading.Tasks;
using Robust.Client.Input;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Windows;

namespace Robust.Client.Graphics.Clyde
{
    partial class Clyde
    {
        internal interface IWindowingImpl
        {
            // Lifecycle stuff
            bool Init();
            void Shutdown();

            // Window loop
            void EnterWindowLoop();
            void PollEvents();
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
                WindowCreateParameters parameters,
                WindowReg? owner);

            void WindowDestroy(WindowReg reg);
            void WindowSetTitle(WindowReg window, string title);
            void WindowSetMonitor(WindowReg window, IClydeMonitor monitor);
            void WindowSetSize(WindowReg window, Vector2i size);
            void WindowSetVisible(WindowReg window, bool visible);
            void WindowRequestAttention(WindowReg window);

            // Keyboard
            string? KeyGetName(Keyboard.Key key);

            // Clipboard
            Task<string> ClipboardGetText(WindowReg mainWindow);
            void ClipboardSetText(WindowReg mainWindow, string text);

            void UpdateMainWindowMode();

            // Misc
            void RunOnWindowThread(Action a);

            // IME
            void TextInputSetRect(WindowReg reg, UIBox2i rect, int cursor);
            void TextInputStart(WindowReg reg);
            void TextInputStop(WindowReg reg);
            string GetDescription();
        }
    }
}
