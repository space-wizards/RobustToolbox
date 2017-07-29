using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Shared.GameLoader
{
    public static class AssemblyLoader
    {
        /// <summary>
        ///     Runlevels of the Content entry point.
        /// </summary>
        public enum RunLevel
        {
            Error = 0,
            Init = 1
        }

        private static readonly List<ModInfo> _mods = new List<ModInfo>();

        private static readonly List<string> _typeWhiteList = new List<string>()
        {
            // engine assemblies
            "SS14.Shared.",
            "SS14.Client.",
            "SS14.Server.",

            // base type assemblies
            typeof(System.Object).FullName
        };

        /// <summary>
        ///     Gets an assembly by name from the given appdomain.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Assembly GetAssemblyByName(this AppDomain domain, string name)
        {
            return domain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == name);
            
        }

        /// <summary>
        ///     Loads and assembly into the current AppDomain.
        /// </summary>
        /// <typeparam name="T">The type of the entry point to search for.</typeparam>
        /// <param name="assembly">Byte array of the assembly.</param>
        /// <param name="symbols">Optional byte array of the debug symbols.</param>
        public static void LoadGameAssembly<T>(byte[] assembly, byte[] symbols = null)
            where T : GameShared
        {
            #region Sandbox

            //TODO: These should prob be convars
            const bool EnableTypeCheck = true;
            const bool DumpContentTypes = true;
            
            using (MemoryStream asmDefStream = new MemoryStream(assembly))
            {
                var asmDef = AssemblyDefinition.ReadAssembly(asmDefStream);

                if(DumpContentTypes)
                {
                    foreach (var typeRef in asmDef.MainModule.GetTypeReferences())
                    {
                        Logger.Info($"[RES] RefType: {typeRef.FullName}");
                    }
                }

                if(EnableTypeCheck)
                {
                    foreach (var item in asmDef.MainModule.GetTypeReferences())
                    {
                        // assemblies are guilty until proven innocent.
                        var safe = false;
                        foreach (var typeName in _typeWhiteList)
                        {
                            if(item.FullName.StartsWith(typeName))
                            {
                                safe = true;
                            }
                        }

                        if (safe)
                            continue;

                        Logger.Error($"[RES] Cannot load {asmDef.MainModule.Name}, {item.FullName} is not whitelisted.");
                        return;
                    }
                }

            }

            #endregion Sandbox

            var mod = new ModInfo();

            if (symbols != null)
                mod.GameAssembly = AppDomain.CurrentDomain.Load(assembly, symbols);
            else
                mod.GameAssembly = AppDomain.CurrentDomain.Load(assembly);

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(mod.GameAssembly);

            var entryPoints = mod.GameAssembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t)).ToArray();

            if (entryPoints.Length == 0)
                Logger.Log($"[RES] Assembly has no entry points: {mod.GameAssembly.FullName}");

            foreach (var entryPoint in entryPoints)
                mod.EntryPoints.Add(Activator.CreateInstance(entryPoint) as T);

            _mods.Add(mod);
        }

        /// <summary>
        ///     Broadcasts a run level change to all loaded entry point.
        /// </summary>
        /// <param name="level">New level</param>
        public static void BroadcastRunLevel(RunLevel level)
        {
            foreach (var mod in _mods)
            foreach (var entry in mod.EntryPoints)
                switch (level)
                {
                    case RunLevel.Init:
                        entry.Init();
                        break;
                    default:
                        Logger.Error($"[RES] Unknown RunLevel: {level}");
                        break;
                }
        }

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
