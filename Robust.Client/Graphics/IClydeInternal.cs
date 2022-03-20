using System;
using System.Collections.Generic;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics
{
    internal interface IClydeInternal : IClyde, IClipboardManager
    {
        // Basic main loop hooks.
        void Render();
        void FrameProcess(FrameEventArgs eventArgs);
        void ProcessInput(FrameEventArgs frameEventArgs);

        // Init.
        bool SeparateWindowThread { get; }
        bool InitializePreWindowing();
        void EnterWindowLoop();
        bool InitializePostWindowing();
        void Ready();
        void TerminateWindowLoop();

        event Action<TextEventArgs> TextEntered;
        event Action<MouseMoveEventArgs> MouseMove;
        event Action<MouseEnterLeaveEventArgs> MouseEnterLeave;
        event Action<KeyEventArgs> KeyUp;
        event Action<KeyEventArgs> KeyDown;
        event Action<MouseWheelEventArgs> MouseWheel;
        event Action<WindowRequestClosedEventArgs> CloseWindow;
        event Action<WindowDestroyedEventArgs> DestroyWindow;

        ClydeHandle LoadShader(ParsedShader shader, string? name = null, Dictionary<string,string>? defines = null);

        void ReloadShader(ClydeHandle handle, ParsedShader newShader);

        /// <summary>
        ///     Creates a new instance of a shader.
        /// </summary>
        /// <param name="handle">The handle of the loaded shader as returned by <see cref="LoadShader"/>.</param>
        ShaderInstance InstanceShader(ClydeHandle handle);

        /// <summary>
        ///     This is purely a hook for <see cref="IInputManager"/>, use that instead.
        /// </summary>
        ScreenCoordinates MouseScreenPosition { get; }

        IClydeDebugInfo DebugInfo { get; }

        IClydeDebugStats DebugStats { get; }

        Texture GetStockTexture(ClydeStockTexture stockTexture);

        ClydeDebugLayers DebugLayers { get; set; }

        string GetKeyName(Keyboard.Key key);

        void Shutdown();

        /// <returns>Null if not running on X11.</returns>
        uint? GetX11WindowId();

        void RunActionOnWindowThread(Action action);

        void RegisterGridEcsEvents();
    }
}
