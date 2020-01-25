using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Run levels of the Content entry point.
    /// </summary>
    public enum ModRunLevel
    {
        Error = 0,
        Init = 1,
        PostInit = 2,
    }

    /// <summary>
    ///     Levels at which point the content assemblies are getting updates.
    /// </summary>
    public enum ModUpdateLevel
    {
        /// <summary>
        ///     This update is called before the main state manager on process frames.
        /// </summary>
        PreEngine,

        /// <summary>
        ///     This update is called before the main state manager on render frames, thus only applies to the client.
        /// </summary>
        FramePreEngine,

        /// <summary>
        ///     This update is called after the main state manager on process frames.
        /// </summary>
        PostEngine,

        /// <summary>
        ///     This update is called after the main state manager on render frames, thus only applies to the client.
        /// </summary>
        FramePostEngine,
    }

    /// <summary>
    ///     Class for managing the loading of assemblies into the engine.
    /// </summary>
    internal class ModLoader : IModLoader, IDisposable
    {
#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager;
        [Dependency] private readonly IResourceManager _resourceManager;
        [Dependency] private readonly ILogManager _logManager;
#pragma warning restore 649

        private ModuleTestingCallbacks _testingCallbacks;

        /// <summary>
        ///     Loaded assemblies.
        /// </summary>
        private readonly List<ModInfo> _mods = new List<ModInfo>();

        private readonly List<Assembly> _sideModules = new List<Assembly>();

        private readonly AssemblyLoadContext _loadContext;

        private readonly object _lock = new object();


        public ModLoader()
        {
            // Imma just turn on collectible assemblies for the heck of it.
            // Even though we don't need it yet.
            _loadContext = new AssemblyLoadContext(null, true);

            _loadContext.Resolving += ResolvingAssembly;
        }

        public virtual void LoadGameAssembly<T>(Stream assembly, Stream symbols = null)
            where T : GameShared
        {
            // TODO: Re-enable type check when it's not just a giant pain in the butt.
            // It slows down development too much and we need other things like System.Type fixed
            // before it can reasonably be re-enabled.
            AssemblyTypeChecker.DisableTypeCheck = true;
            AssemblyTypeChecker.DumpTypes = false;
            if (!AssemblyTypeChecker.CheckAssembly(assembly))
                return;

            assembly.Position = 0;

            var gameAssembly = _loadContext.LoadFromStream(assembly, symbols);

            InitMod<T>(gameAssembly);
        }

        public virtual void LoadGameAssembly<T>(string diskPath)
            where T : GameShared
        {
            // TODO: Re-enable type check when it's not just a giant pain in the butt.
            // It slows down development too much and we need other things like System.Type fixed
            // before it can reasonably be re-enabled.
            AssemblyTypeChecker.DisableTypeCheck = true;
            AssemblyTypeChecker.DumpTypes = false;

            if (!AssemblyTypeChecker.CheckAssembly(diskPath))
                return;

            InitMod<T>(Assembly.LoadFrom(diskPath));
        }

        protected void InitMod<T>(Assembly assembly) where T : GameShared
        {
            var mod = new ModInfo {GameAssembly = assembly};

            _reflectionManager.LoadAssemblies(mod.GameAssembly);

            var entryPoints = mod.GameAssembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t)).ToArray();

            if (entryPoints.Length == 0)
                Logger.WarningS("res", $"Assembly has no entry points: {mod.GameAssembly.FullName}");

            foreach (var entryPoint in entryPoints)
            {
                var entryPointInstance = (T) Activator.CreateInstance(entryPoint);
                entryPointInstance.SetTestingCallbacks(_testingCallbacks);
                mod.EntryPoints.Add(entryPointInstance);
            }

            _mods.Add(mod);
        }

        public void BroadcastRunLevel(ModRunLevel level)
        {
            foreach (var mod in _mods)
            {
                foreach (var entry in mod.EntryPoints)
                {
                    switch (level)
                    {
                        case ModRunLevel.Init:
                            entry.Init();
                            break;
                        case ModRunLevel.PostInit:
                            entry.PostInit();
                            break;
                        default:
                            Logger.ErrorS("res", $"Unknown RunLevel: {level}");
                            break;
                    }
                }
            }
        }

        public void BroadcastUpdate(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            foreach (var entrypoint in _mods.SelectMany(m => m.EntryPoints))
            {
                entrypoint.Update(level, frameEventArgs);
            }
        }

        public virtual bool TryLoadAssembly<T>(IResourceManager resMan, string assemblyName)
            where T : GameShared
        {
            var dllPath = new ResourcePath($@"/Assemblies/{assemblyName}.dll");
            // To prevent breaking debugging on Rider, try to load from disk if possible.
#if DEBUG
            if (resMan.TryGetDiskFilePath(dllPath, out var path))
            {
                Logger.DebugS("srv", $"Loading {assemblyName} DLL");
                try
                {
                    LoadGameAssembly<T>(path);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.ErrorS("srv", $"Exception loading DLL {assemblyName}.dll: {e.ToStringBetter()}");
                    return false;
                }
            }
#endif
            if (resMan.TryContentFileRead(dllPath, out var gameDll))
            {
                Logger.DebugS("srv", $"Loading {assemblyName} DLL");

#if DEBUG
                // see if debug info is present
                if (resMan.TryContentFileRead(new ResourcePath($@"/Assemblies/{assemblyName}.pdb"), out var gamePdb))
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        LoadGameAssembly<T>(gameDll, gamePdb);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("srv", $"Exception loading DLL {assemblyName}.dll: {e.ToStringBetter()}");
                        return false;
                    }
                }
#endif

                try
                {
                    // load the assembly into the process, and bootstrap the GameServer entry point.
                    LoadGameAssembly<T>(gameDll);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.ErrorS("srv", $"Exception loading DLL {assemblyName}.dll: {e.ToStringBetter()}");
                    return false;
                }
            }

            Logger.WarningS("eng", $"Could not load {assemblyName} DLL: {dllPath} does not exist in the VFS.");
            return false;
        }

        public void SetModuleBaseCallbacks(ModuleTestingCallbacks testingCallbacks)
        {
            _testingCallbacks = testingCallbacks;
        }

        private Assembly ResolvingAssembly(AssemblyLoadContext context, AssemblyName name)
        {
            try
            {
                lock (_lock)
                {
                    _logManager.GetSawmill("res").Debug("ResolvingAssembly {0}", name);

                    // Try main modules.
                    foreach (var mod in _mods)
                    {
                        if (mod.GameAssembly.FullName == name.FullName)
                        {
                            return mod.GameAssembly;
                        }
                    }

                    foreach (var assembly in _sideModules)
                    {
                        if (assembly.FullName == name.FullName)
                        {
                            return assembly;
                        }
                    }

                    if (_resourceManager.TryContentFileRead($"/Assemblies/{name.Name}.dll", out var dll))
                    {
                        var assembly = _loadContext.LoadFromStream(dll);
                        _sideModules.Add(assembly);
                        return assembly;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                _logManager.GetSawmill("res").Error("Exception in ResolvingAssembly: {0}", e);
                ExceptionDispatchInfo.Capture(e).Throw();
                throw null; // Unreachable.
            }
        }

        public void Dispose()
        {
            _loadContext.Unload();
        }

        /// <summary>
        ///     Holds info about a loaded assembly.
        /// </summary>
        private class ModInfo
        {
            public ModInfo()
            {
                EntryPoints = new List<GameShared>();
            }

            public Assembly GameAssembly { get; set; }
            public List<GameShared> EntryPoints { get; }
        }
    }
}
