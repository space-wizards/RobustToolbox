using System;
using System.Runtime.Serialization;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed unsafe partial class GlfwWindowingImpl : IWindowingImpl
        {
            [Dependency] private readonly ILogManager _logManager = default!;
            [Dependency] private readonly IConfigurationManager _cfg = default!;

            private readonly Clyde _clyde;

            private readonly ISawmill _sawmill;

            private readonly RefList<GlfwEvent> _glfwEventQueue = new();
            private bool _glfwInitialized;

            public GlfwWindowingImpl(Clyde clyde)
            {
                _clyde = clyde;
                IoCManager.InjectDependencies(this);

                _sawmill = _logManager.GetSawmill("clyde.win");
            }

            public bool Init()
            {
                if (!InitGlfw())
                {
                    return false;
                }

                SetupGlobalCallbacks();
                InitMonitors();
                InitCursors();

                return true;
            }

            public void Shutdown()
            {
                if (_glfwInitialized)
                {
                    Logger.DebugS("clyde.win", "Terminating GLFW.");
                    GLFW.Terminate();
                }
            }

            public void FlushDispose()
            {
                FlushCursorDispose();
            }

            private bool InitGlfw()
            {
                StoreCallbacks();

                GLFW.SetErrorCallback(_errorCallback);
                if (!GLFW.Init())
                {
                    var err = GLFW.GetError(out var desc);
                    _sawmill.Fatal("clyde.win", $"Failed to initialize GLFW! [{err}] {desc}");
                    return false;
                }

                _glfwInitialized = true;
                var version = GLFW.GetVersionString();
                _sawmill.Debug("clyde.win", "GLFW initialized, version: {0}.", version);

                return true;
            }

            private static void OnGlfwError(ErrorCode code, string description)
            {
                Logger.ErrorS("clyde.win.glfw", "GLFW Error: [{0}] {1}", code, description);
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
