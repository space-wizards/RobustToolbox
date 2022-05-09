using System;
using System.Threading;
using Robust.LoaderApi;
using Robust.Shared;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client;

internal partial class GameController
{
    /// <summary>
    ///     Try to cause the launcher to either reconnect to the same server or connect to a new server.
    ///     *The engine will shutdown on success.*
    ///     Will throw an exception if contacting the launcher failed (success indicates it is now the launcher's responsibility).
    ///     To redial the same server, retrieve the server's address from `LaunchState.Ss14Address`.
    /// </summary>
    /// <param name="address">The server address, such as "ss14://localhost:1212/".</param>
    /// <param name="text">Informational text on the cause of the reconnect. Empty or null gives a default reason.</param>
    public void Redial(string address, string? text = null)
    {
        // -- ATTENTION, YE WOULD-BE TRESPASSERS! --
        // This code is the least testable code ever (because it's in-engine code that requires the Launcher), so don't make it do too much.
        // This checks for some obvious requirements and then forwards to RedialApi which does the actual work.
        // Testing of RedialApi is doable in the SS14.Launcher project, this is why it has a fake RobustToolbox build.
        // -- THANK YE, NOW KINDLY MOVE ALONG! --

        // We don't ever want to successfully redial more than once, and we want to shutdown once we've redialled.
        // Otherwise abuse could happen.
        DebugTools.AssertNotNull(_mainLoop);

        Logger.Info($"Attempting redial of {address}: {text ?? "no reason given"}");

        if (!_mainLoop!.Running)
        {
            throw new Exception("Attempted a redial during shutdown, this is not acceptable - redial first and if you succeed it'll shutdown anyway.");
        }

        if (_loaderArgs == null)
        {
            throw new Exception("Attempted a redial when the game was not run from the launcher (_loaderArgs == null)");
        }

        if (_loaderArgs!.RedialApi == null)
        {
            throw new Exception("Attempted a redial when redialling was not supported by the loader (Outdated launcher?)");
        }

        _loaderArgs!.RedialApi!.Redial(new Uri(address), text ?? "");

        Shutdown("Redial");
    }
}

