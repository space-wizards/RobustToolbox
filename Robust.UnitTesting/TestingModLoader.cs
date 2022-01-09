using System;
using System.IO;
using System.Reflection;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.UnitTesting
{
    internal sealed class TestingModLoader : BaseModLoader, IModLoaderInternal
    {
        public Assembly[] Assemblies { get; set; } = Array.Empty<Assembly>();

        public bool TryLoadModulesFrom(ResourcePath mountPath, string filterPrefix)
        {
            foreach (var assembly in Assemblies)
            {
                InitMod(assembly);
            }

            return true;
        }

        public void LoadGameAssembly(Stream assembly, Stream? symbols = null, bool skipVerify = false)
        {
            throw new NotSupportedException();
        }

        public void LoadGameAssembly(string diskPath, bool skipVerify = false)
        {
            throw new NotSupportedException();
        }

        public bool TryLoadAssembly(string assemblyName)
        {
            throw new NotSupportedException();
        }

        public void SetUseLoadContext(bool useLoadContext)
        {
            // Nada.
        }

        public void SetEnableSandboxing(bool sandboxing)
        {
            // Nada.
        }

        public Func<string, Stream?>? VerifierExtraLoadHandler { get; set; }

        public void AddEngineModuleDirectory(string dir)
        {
            // Only used for ILVerify, not necessary.
        }

        public event ExtraModuleLoad? ExtraModuleLoaders;
    }
}
