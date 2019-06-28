using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
    internal sealed class ModLoader : IModLoader
    {
#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager;
#pragma warning restore 649

        static ModLoader()
        {
            // Necessary to make the assembly loader not choke on Windows on release builds,
            // it seems like.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveMissingAssembly;
        }

        private ModuleTestingCallbacks _testingCallbacks;

        /// <summary>
        ///     Loaded assemblies.
        /// </summary>
        private readonly List<ModInfo> _mods = new List<ModInfo>();

        public void LoadGameAssembly<T>(byte[] assembly, byte[] symbols = null)
            where T : GameShared
        {
            // TODO: Re-enable type check when it's not just a giant pain in the butt.
            // It slows down development too much and we need other things like System.Type fixed
            // before it can reasonably be re-enabled.
            AssemblyTypeChecker.DisableTypeCheck = true;
            AssemblyTypeChecker.DumpTypes = true;
            if (!AssemblyTypeChecker.CheckAssembly(assembly))
                return;

            var gameAssembly = symbols != null
                ? AppDomain.CurrentDomain.Load(assembly, symbols)
                : AppDomain.CurrentDomain.Load(assembly);

            InitMod<T>(gameAssembly);
        }

        public void LoadGameAssembly<T>(string diskPath)
            where T : GameShared
        {
            // TODO: Re-enable type check when it's not just a giant pain in the butt.
            // It slows down development too much and we need other things like System.Type fixed
            // before it can reasonably be re-enabled.
            AssemblyTypeChecker.DisableTypeCheck = true;
            AssemblyTypeChecker.DumpTypes = true;
            if (!AssemblyTypeChecker.CheckAssembly(File.ReadAllBytes(diskPath)))
                return;

            InitMod<T>(Assembly.LoadFrom(diskPath));
        }

        private void InitMod<T>(Assembly assembly) where T : GameShared
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

        public void BroadcastUpdate(ModUpdateLevel level, float frameTime)
        {
            foreach (var entrypoint in _mods.SelectMany(m => m.EntryPoints))
            {
                entrypoint.Update(level, frameTime);
            }
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

        public bool TryLoadAssembly<T>(IResourceManager resMan, string assemblyName)
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
                        LoadGameAssembly<T>(gameDll.ToArray(), gamePdb.ToArray());
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
                    LoadGameAssembly<T>(gameDll.ToArray());
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

        private static Assembly ResolveMissingAssembly(object sender, ResolveEventArgs args)
        {
            var modLoader = IoCManager.Resolve<IModLoader>();
            if (!(modLoader is ModLoader me))
            {
                return null;
            }

            foreach (var mod in me._mods)
            {
                if (mod.GameAssembly.FullName == args.Name)
                {
                    return mod.GameAssembly;
                }
            }

            return null;
        }
    }
}
