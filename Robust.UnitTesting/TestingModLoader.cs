using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.UnitTesting
{
    internal sealed class TestingModLoader : BaseModLoader, IModLoaderInternal
    {
        public Assembly[] Assemblies { get; set; } = Array.Empty<Assembly>();

        public bool TryLoadModulesFrom(ResPath mountPath, string filterPrefix)
        {
            return TryLoadModules(Array.Empty<ResPath>());
        }

        public bool TryLoadModules(IEnumerable<ResPath> paths)
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
#pragma warning disable CS0067 // Needed by interface
        public event ExtraModuleLoad? ExtraModuleLoaders;
#pragma warning restore CS0067
    }
}
