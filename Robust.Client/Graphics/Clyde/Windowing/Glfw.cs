using System;
using System.Runtime.Serialization;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Input;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed partial class GlfwWindowingImpl : IWindowingImpl
        {
            [Dependency] private readonly ILogManager _logManager = default!;
            [Dependency] private readonly IConfigurationManager _cfg = default!;
            [Dependency] private readonly ILocalizationManager _loc = default!;
            [Dependency] private readonly IInputManager _inputManager = default!;

            private readonly Clyde _clyde;

            private readonly ISawmill _sawmill;
            private readonly ISawmill _sawmillGlfw;

            private bool _glfwInitialized;
            private bool _win32Experience;

            public GlfwWindowingImpl(Clyde clyde)
            {
                _clyde = clyde;
                IoCManager.InjectDependencies(this);

                _sawmill = _logManager.GetSawmill("clyde.win");
                _sawmillGlfw = _logManager.GetSawmill("clyde.win.glfw");
            }

            public bool Init()
            {
#if DEBUG
                _cfg.OnValueChanged(CVars.DisplayWin32Experience, b => _win32Experience = b, true);
#endif
                _cfg.OnValueChanged(CVars.DisplayUSQWERTYHotkeys, ReInitKeyMap);

                InitChannels();

                if (!InitGlfw())
                {
                    return false;
                }

                SetupGlobalCallbacks();
                InitMonitors();
                InitCursors();
                InitKeyMap();

                return true;
            }

            public void Shutdown()
            {
                if (_glfwInitialized)
                {
                    _sawmill.Debug("Terminating GLFW.");
                    _cfg.UnsubValueChanged(CVars.DisplayUSQWERTYHotkeys, ReInitKeyMap);
                    GLFW.Terminate();
                }
            }

            public void FlushDispose()
            {
                // Not currently used
            }

            private void ReInitKeyMap(bool onValueChanged)
            {
                InitKeyMap();
                _inputManager.InputModeChanged();
            }

            private bool InitGlfw()
            {
                StoreCallbacks();

                GLFW.SetErrorCallback(_errorCallback);
                if (!GLFW.Init())
                {
                    var err = GLFW.GetError(out var desc);
                    _sawmill.Fatal($"Failed to initialize GLFW! [{err}] {desc}");
                    return false;
                }

                _glfwInitialized = true;
                var version = GLFW.GetVersionString();
                _sawmill.Debug("GLFW initialized, version: {0}.", version);

                return true;
            }

            private void OnGlfwError(ErrorCode code, string description)
            {
                _sawmillGlfw.Error("GLFW Error: [{0}] {1}", code, description);
            }

            [Serializable]
            public class GlfwException : Exception
            {
                public GlfwException()
                {
                }

                public GlfwException(string message) : base(message)
                {
                }

                public GlfwException(string message, Exception inner) : base(message, inner)
                {
                }

                protected GlfwException(
                    SerializationInfo info,
                    StreamingContext context) : base(info, context)
                {
                }
            }
        }
    }
}
