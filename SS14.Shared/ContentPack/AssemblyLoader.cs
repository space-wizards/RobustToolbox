using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SS14.Shared.Log;

namespace SS14.Shared.GameLoader
{
    public static class AssemblyLoader
    {
        public enum RunLevel
        {
            Error = 0,
            Init = 1,
        }

        private static List<ModInfo> _mods = new List<ModInfo>();

        [Obsolete]
        public static Assembly RelativeLoadFrom(string path)
        {
            string assemblyDir = Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath);
            return Assembly.Load(Path.Combine(assemblyDir, path));
        }

        public static Assembly GetAssemblyByName(this AppDomain domain, string name)
        {
            return domain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == name);
        }

        public static void LoadGameAssembly<T>(byte[] file)
            where T : GameShared
        {
            //TODO: enforce sandbox in assembly

            var mod = new ModInfo();
            mod.GameAssembly = Assembly.Load(file);

            var entryPoints = mod.GameAssembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t)).ToArray();

            if(entryPoints.Length == 0)
                Logger.Log($"[RES] Assembly has no entry points: {mod.GameAssembly.FullName}");

            foreach (var entryPoint in entryPoints)
            {
                mod.EntryPoints.Add(Activator.CreateInstance(entryPoint) as T);
            }
            
            _mods.Add(mod);
        }

        public static void BroadcastRunLevel(RunLevel level)
        {
            foreach (var mod in _mods)
            {
                foreach (var entry in mod.EntryPoints)
                {
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
            }
        }

        private class ModInfo
        {
            public Assembly GameAssembly { get; set; }
            public List<GameShared> EntryPoints { get; set; }

            public ModInfo()
            {
                EntryPoints = new List<GameShared>();
            }
        }
    }
}
