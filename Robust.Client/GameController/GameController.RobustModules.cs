using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Robust.Client.WebViewHook;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client
{
    internal sealed partial class GameController
    {
        private void LoadOptionalRobustModules(DisplayMode mode, ResourceManifestData manifest)
        {
            foreach (var module in manifest.Modules)
            {
                switch (module)
                {
                    case "Robust.Client.WebView":
                        LoadRobustWebView(mode);
                        break;
                    default:
                        _logger.Error($"Unknown Robust module: {module}");
                        return;
                }
            }
        }

        private void LoadRobustWebView(GameController.DisplayMode mode)
        {
            _logger.Debug("Loading Robust.Client.WebView");

            var alc = CreateModuleLoadContext("Robust.Client.WebView");
            var assembly = alc.LoadFromAssemblyName(new AssemblyName("Robust.Client.WebView"));
            var attribute = assembly.GetCustomAttribute<WebViewManagerImplAttribute>()!;
            DebugTools.AssertNotNull(attribute);

            var managerType = attribute.ImplementationType;
            _webViewHook = (IWebViewManagerHook)Activator.CreateInstance(managerType)!;
            _webViewHook.PreInitialize(_dependencyCollection, mode);

            _logger.Debug("Done pre-initializing Robust.Client.WebView");
        }

        /// <summary>
        /// Creates an <see cref="AssemblyLoadContext"/> that loads from an engine module directory.
        /// </summary>
        private AssemblyLoadContext CreateModuleLoadContext(string moduleName)
        {
            var sawmill = _logManager.GetSawmill("robust.mod");

            var alc = new AssemblyLoadContext(moduleName);
            var envVarName = $"ROBUST_MODULE_{moduleName.ToUpperInvariant().Replace('.', '_')}";
            var envVar = Environment.GetEnvironmentVariable(envVarName);
            if (string.IsNullOrEmpty(envVar))
            {
                sawmill.Debug("Module {ModuleName} has no path override specified", moduleName);
                return alc;
            }

            sawmill.Debug("Path for module {ModuleName} is {ModulePath}", moduleName, envVar);

            alc.Resolving += (_, name) =>
            {
                sawmill.Debug("Loading {AssemblyName} from module {ModuleName}", name.ToString(), moduleName);
                var assemblyPath = Path.Combine(envVar, $"{name.Name}.dll");
                if (!File.Exists(assemblyPath))
                    return null;

                return alc.LoadFromAssemblyPath(assemblyPath);
            };

            _modLoader.ExtraModuleLoaders += name =>
            {
                foreach (var assembly in alc.Assemblies)
                {
                    var assemblyName = assembly.GetName();
                    if (assemblyName.Name == name.Name)
                    {
                        sawmill.Debug("Resolved {ResolvingAssembly} as assembly {ResolvedAssembly} from {ModuleName}", name.ToString(), assemblyName.ToString(), moduleName);
                        return assembly;
                    }
                }

                return null;
            };

            _modLoader.AddEngineModuleDirectory(envVar);

            return alc;
        }
    }
}
