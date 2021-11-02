using System;
using System.Reflection;
using Robust.Client.WebViewHook;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client
{
    internal sealed partial class GameController
    {
        private void LoadOptionalRobustModules()
        {
            // In the future, this manifest should be loaded somewhere else and used for more parts of init.
            // For now, this is fine.
            var manifest = LoadResourceManifest();

            foreach (var module in manifest.Modules)
            {
                switch (module)
                {
                    case "Robust.Client.WebView":
                        LoadRobustWebView();
                        break;
                    default:
                        Logger.Error($"Unknown Robust module: {module}");
                        return;
                }
            }
        }

        private void LoadRobustWebView()
        {
            Logger.Debug("Loading Robust.Client.WebView");

            var assembly = LoadRobustModuleAssembly("Robust.Client.WebView");
            var attribute = assembly.GetCustomAttribute<WebViewManagerImplAttribute>()!;
            DebugTools.AssertNotNull(attribute);

            var managerType = attribute.ImplementationType;
            _webViewHook = (IWebViewManagerHook)Activator.CreateInstance(managerType)!;
            _webViewHook.Initialize();

            Logger.Debug("Done initializing Robust.Client.WebView");
        }

        private Assembly LoadRobustModuleAssembly(string assemblyName)
        {
            // TODO: Launcher distribution and all that stuff.
            return Assembly.Load(assemblyName);
        }
    }
}
