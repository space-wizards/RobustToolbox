using Robust.Client.Graphics;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Input;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.Interfaces.Graphics
{
    internal interface IClydeInternal : IClyde, IClipboardManager
    {
        // Basic main loop hooks.
        void Render();
        void FrameProcess(FrameEventArgs eventArgs);
        void ProcessInput(FrameEventArgs frameEventArgs);

        // Init.
        bool Initialize();
        void Ready();

        ClydeHandle LoadShader(ParsedShader shader, string? name = null);

        void ReloadShader(ClydeHandle handle, ParsedShader newShader);

        /// <summary>
        ///     Creates a new instance of a shader.
        /// </summary>
        /// <param name="handle">The handle of the loaded shader as returned by <see cref="LoadShader"/>.</param>
        ShaderInstance InstanceShader(ClydeHandle handle);

        /// <summary>
        ///     This is purely a hook for <see cref="IInputManager"/>, use that instead.
        /// </summary>
        Vector2 MouseScreenPosition { get; }

        IClydeDebugInfo DebugInfo { get; }

        IClydeDebugStats DebugStats { get; }

        Texture GetStockTexture(ClydeStockTexture stockTexture);

        ClydeDebugLayers DebugLayers { get; set; }

        string GetKeyName(Keyboard.Key key);
        string GetKeyNameScanCode(int scanCode);

        int GetKeyScanCode(Keyboard.Key key);
        void Shutdown();

        /// <returns>Null if not running on X11.</returns>
        uint? GetX11WindowId();
    }
}
