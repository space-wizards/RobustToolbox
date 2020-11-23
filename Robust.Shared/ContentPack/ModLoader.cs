using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Threading;
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
        PreInit = 3,
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
    internal class ModLoader : IModLoaderInternal, IDisposable, IPostInjectInit
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private readonly List<ModuleTestingCallbacks> _testingCallbacks = new List<ModuleTestingCallbacks>();
        private AssemblyTypeChecker _typeChecker = default!;

        /// <summary>
        ///     Loaded assemblies.
        /// </summary>
        private readonly List<ModInfo> _mods = new List<ModInfo>();

        // List of extra assemblies side-loaded from the /Assemblies/ mounted path.
        private readonly List<Assembly> _sideModules = new List<Assembly>();

        private readonly AssemblyLoadContext _loadContext;

        private readonly object _lock = new object();

        private static int _modLoaderId;

        private bool _useLoadContext = true;
        private bool _sandboxingEnabled;

        public ModLoader()
        {
            var id = Interlocked.Increment(ref _modLoaderId);
            // Imma just turn on collectible assemblies for the heck of it.
            // Even though we don't need it yet.
            _loadContext = new AssemblyLoadContext($"ModLoader-{id}", true);

            _loadContext.Resolving += ResolvingAssembly;
        }

        void IPostInjectInit.PostInject()
        {
            _typeChecker = new AssemblyTypeChecker(_resourceManager);
        }

        public void SetUseLoadContext(bool useLoadContext)
        {
            _useLoadContext = useLoadContext;
            Logger.DebugS("res", "{0} assembly load context", useLoadContext ? "ENABLING" : "DISABLING");
        }

        public void SetEnableSandboxing(bool sandboxing)
        {
            _sandboxingEnabled = sandboxing;
            _typeChecker.VerifyIL = sandboxing;
            _typeChecker.DisableTypeCheck = !sandboxing;
            Logger.DebugS("res", "{0} sandboxing", sandboxing ? "ENABLING" : "DISABLING");
        }

        public bool IsContentAssembly(Assembly typeAssembly)
        {
            foreach (var mod in _mods)
            {
                if (mod.GameAssembly == typeAssembly)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void LoadGameAssembly<T>(Stream assembly, Stream? symbols = null)
            where T : GameShared
        {
            if (!_typeChecker.CheckAssembly(assembly))
                return;

            assembly.Position = 0;

            Assembly gameAssembly;
            if (_useLoadContext)
            {
                gameAssembly = _loadContext.LoadFromStream(assembly, symbols);
            }
            else
            {
                gameAssembly = Assembly.Load(assembly.CopyToArray(), symbols?.CopyToArray());
            }

            InitMod<T>(gameAssembly);
        }

        public virtual void LoadGameAssembly<T>(string diskPath)
            where T : GameShared
        {
            if (!_typeChecker.CheckAssembly(diskPath))
            {
                throw new TypeCheckFailedException();
            }

            Assembly assembly;
            if (_useLoadContext)
            {
                assembly = _loadContext.LoadFromAssemblyPath(diskPath);
            }
            else
            {
                assembly = Assembly.LoadFrom(diskPath);
            }

            InitMod<T>(assembly);
        }

        public Assembly GetAssembly(string name)
        {
            return _mods.Select(p => p.GameAssembly).Single(p => p.GetName().Name == name);
        }

        protected void InitMod<T>(Assembly assembly) where T : GameShared
        {
            var mod = new ModInfo(assembly);

            _reflectionManager.LoadAssemblies(mod.GameAssembly);

            var entryPoints = mod.GameAssembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t)).ToArray();

            if (entryPoints.Length == 0)
                Logger.WarningS("res", $"Assembly has no entry points: {mod.GameAssembly.FullName}");

            foreach (var entryPoint in entryPoints)
            {
                var entryPointInstance = (T) Activator.CreateInstance(entryPoint)!;
                if (_testingCallbacks != null)
                {
                    entryPointInstance.SetTestingCallbacks(_testingCallbacks);
                }

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
                        case ModRunLevel.PreInit:
                            entry.PreInit();
                            break;
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
            foreach (var module in _mods)
            {
                foreach (var entryPoint in module.EntryPoints)
                {
                    entryPoint.Update(level, frameEventArgs);
                }
            }
        }

        public virtual bool TryLoadAssembly<T>(IResourceManager resMan, string assemblyName)
            where T : GameShared
        {
            var dllPath = new ResourcePath($@"/Assemblies/{assemblyName}.dll");
            // To prevent breaking debugging on Rider, try to load from disk if possible.
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

            if (resMan.TryContentFileRead(dllPath, out var gameDll))
            {
                Logger.DebugS("srv", $"Loading {assemblyName} DLL");

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
            _testingCallbacks.Add(testingCallbacks);
        }

        private Assembly? ResolvingAssembly(AssemblyLoadContext context, AssemblyName name)
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

                    // Do not allow sideloading when sandboxing is enabled.
                    // Side loaded assemblies would not be checked for sandboxing currently, so we can't have that.
                    if (!_sandboxingEnabled)
                    {
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
            public ModInfo(Assembly gameAssembly)
            {
                GameAssembly = gameAssembly;
                EntryPoints = new List<GameShared>();
            }

            public Assembly GameAssembly { get; }
            public List<GameShared> EntryPoints { get; }
        }
    }
}
