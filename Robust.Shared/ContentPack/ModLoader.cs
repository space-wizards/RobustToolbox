using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Threading;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Class for managing the loading of assemblies into the engine.
    /// </summary>
    internal sealed class ModLoader : BaseModLoader, IModLoaderInternal, IDisposable, IPostInjectInit
    {
        [Dependency] private readonly IResourceManagerInternal _res = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private AssemblyTypeChecker _typeChecker = default!;

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
            _typeChecker = new AssemblyTypeChecker(_res);
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

        public bool TryLoadModulesFrom(ResourcePath mountPath, string filterPrefix)
        {
            var files = new List<(string name, ResourcePath Path, string[] references)>();

            // Find all modules we want to load.
            foreach (var filePath in _res.ContentFindRelativeFiles(mountPath)
                .Where(p => !p.ToString().Contains('/') && p.Filename.StartsWith(filterPrefix) && p.Extension == "dll"))
            {
                var fullPath = mountPath / filePath;
                Logger.DebugS("res.mod", $"Found module '{fullPath}'");

                var asmFile = _res.ContentFileRead(fullPath);
                var (asmRefs, asmName) = GetAssemblyReferenceData(asmFile);

                files.Add((asmName, fullPath, asmRefs));
            }

            // Actually load them in the order they depend on each other.
            foreach (var path in TopologicalSortModules(files))
            {
                Logger.DebugS("res.mod", $"Loading module: '{path}");
                try
                {
                    // If possible, load from disk path instead.
                    // This probably improves performance or something and makes debugging etc more reliable.
                    if (_res.TryGetDiskFilePath(path, out var diskPath))
                    {
                        LoadGameAssembly(diskPath);
                    }
                    else
                    {
                        var assemblyStream = _res.ContentFileRead(path);
                        var symbolsStream = _res.ContentFileReadOrNull(path.WithExtension("pdb"));
                        LoadGameAssembly(assemblyStream, symbolsStream);
                    }
                }
                catch (Exception e)
                {
                    Logger.ErrorS("srv", $"Exception loading module '{path}':\n{e.ToStringBetter()}");
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<ResourcePath> TopologicalSortModules(
            IEnumerable<(string name, ResourcePath Path, string[] references)> modules)
        {
            var elems = modules.ToDictionary(
                node => node.name,
                node => (node.Path, refs: new HashSet<string>(node.references)));

            // Remove assembly references we aren't sorting for.
            foreach (var (_, set) in elems.Values)
            {
                set.RemoveWhere(r => !elems.ContainsKey(r));
            }

            while (elems.Count > 0)
            {
                var elem = elems.FirstOrNull(x => x.Value.refs.Count == 0);
                if (elem == null)
                {
                    throw new InvalidOperationException(
                        "Found circular dependency in assembly dependency graph");
                }

                elems.Remove(elem.Value.Key);
                foreach (var sElem in elems)
                {
                    sElem.Value.refs.Remove(elem.Value.Key);
                }

                yield return elem.Value.Value.Path;
            }
        }

        private static (string[] refs, string name) GetAssemblyReferenceData(Stream stream)
        {
            using var reader = new PEReader(stream);
            var metaReader = reader.GetMetadataReader();

            var name = metaReader.GetString(metaReader.GetAssemblyDefinition().Name);

            return (metaReader.AssemblyReferences
                .Select(a => metaReader.GetAssemblyReference(a))
                .Select(a => metaReader.GetString(a.Name)).ToArray(), name);
        }

        public void LoadGameAssembly(Stream assembly, Stream? symbols = null)
        {
            if (!_typeChecker.CheckAssembly(assembly))
            {
                throw new TypeCheckFailedException();
            }

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

            InitMod(gameAssembly);
        }

        public void LoadGameAssembly(string diskPath)
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

            InitMod(assembly);
        }

        public bool TryLoadAssembly(string assemblyName)
        {
            var dllPath = new ResourcePath($@"/Assemblies/{assemblyName}.dll");
            // To prevent breaking debugging on Rider, try to load from disk if possible.
            if (_res.TryGetDiskFilePath(dllPath, out var path))
            {
                Logger.DebugS("srv", $"Loading {assemblyName} DLL");
                try
                {
                    LoadGameAssembly(path);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.ErrorS("srv", $"Exception loading DLL {assemblyName}.dll: {e.ToStringBetter()}");
                    return false;
                }
            }

            if (_res.TryContentFileRead(dllPath, out var gameDll))
            {
                Logger.DebugS("srv", $"Loading {assemblyName} DLL");

                // see if debug info is present
                if (_res.TryContentFileRead(new ResourcePath($@"/Assemblies/{assemblyName}.pdb"),
                    out var gamePdb))
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        LoadGameAssembly(gameDll, gamePdb);
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
                    LoadGameAssembly(gameDll);
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

        private Assembly? ResolvingAssembly(AssemblyLoadContext context, AssemblyName name)
        {
            try
            {
                lock (_lock)
                {
                    _logManager.GetSawmill("res").Debug("ResolvingAssembly {0}", name);

                    // Try main modules.
                    foreach (var mod in Mods)
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

                        if (_res.TryContentFileRead($"/Assemblies/{name.Name}.dll", out var dll))
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
    }
}
