using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Class for managing the loading of assemblies into the engine.
    /// </summary>
    internal sealed class ModLoader : BaseModLoader, IModLoaderInternal, IDisposable
    {
        [Dependency] private readonly IResourceManagerInternal _res = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        // List of extra assemblies side-loaded from the /Assemblies/ mounted path.
        private readonly List<Assembly> _sideModules = new();

        private readonly AssemblyLoadContext _loadContext;

        private readonly object _lock = new();

        private static int _modLoaderId;

        private bool _useLoadContext = true;

        /// <inheritdoc />
        public bool SandboxingEnabled { get; private set; }

        public ModLoader()
        {
            var id = Interlocked.Increment(ref _modLoaderId);
            // Imma just turn on collectible assemblies for the heck of it.
            // Even though we don't need it yet.
            _loadContext = new AssemblyLoadContext($"ModLoader-{id}", true);

            _loadContext.Resolving += ResolvingAssembly;

            AssemblyLoadContext.Default.Resolving += DefaultOnResolving;
        }

        public void SetUseLoadContext(bool useLoadContext)
        {
            _useLoadContext = useLoadContext;
            Logger.DebugS("res", "{0} assembly load context", useLoadContext ? "ENABLING" : "DISABLING");
        }

        public void SetEnableSandboxing(bool sandboxing)
        {
            SandboxingEnabled = sandboxing;
            Logger.DebugS("res", "{0} sandboxing", sandboxing ? "ENABLING" : "DISABLING");
        }

        public Func<string, Stream?>? VerifierExtraLoadHandler { get; set; }

        public bool TryLoadModulesFrom(ResourcePath mountPath, string filterPrefix)
        {
            var sw = Stopwatch.StartNew();
            Logger.DebugS("res.mod", "LOADING modules");
            var files = new Dictionary<string, (ResourcePath Path, string[] references)>();

            // Find all modules we want to load.
            foreach (var filePath in _res.ContentFindRelativeFiles(mountPath)
                .Where(p => !p.ToString().Contains('/') && p.Filename.StartsWith(filterPrefix) && p.Extension == "dll"))
            {
                var fullPath = mountPath / filePath;
                Logger.DebugS("res.mod", $"Found module '{fullPath}'");

                using var asmFile = _res.ContentFileRead(fullPath);
                var (asmRefs, asmName) = GetAssemblyReferenceData(asmFile);

                if (!files.TryAdd(asmName, (fullPath, asmRefs)))
                {
                    Logger.ErrorS("res.mod", "Found multiple modules with the same assembly name " +
                                             $"'{asmName}', A: {files[asmName].Path}, B: {fullPath}.");
                    return false;
                }
            }

            if (SandboxingEnabled)
            {
                var checkerSw = Stopwatch.StartNew();

                var typeChecker = MakeTypeChecker();

                Parallel.ForEach(files, pair =>
                {
                    var (name, (path, _)) = pair;

                    using var stream = _res.ContentFileRead(path);
                    if (!typeChecker.CheckAssembly(stream))
                    {
                        throw new TypeCheckFailedException($"Assembly {name} failed type checks.");
                    }
                });

                Logger.DebugS("res.mod", $"Verified assemblies in {checkerSw.ElapsedMilliseconds}ms");
            }

            // Actually load them in the order they depend on each other.
            foreach (var path in TopologicalSortModules(files))
            {
                Logger.DebugS("res.mod", $"Loading module: '{path}'");
                try
                {
                    // If possible, load from disk path instead.
                    // This probably improves performance or something and makes debugging etc more reliable.
                    if (_res.TryGetDiskFilePath(path, out var diskPath))
                    {
                        LoadGameAssembly(diskPath, skipVerify: true);
                    }
                    else
                    {
                        using var assemblyStream = _res.ContentFileRead(path);
                        using var symbolsStream = _res.ContentFileReadOrNull(path.WithExtension("pdb"));
                        LoadGameAssembly(assemblyStream, symbolsStream, skipVerify: true);
                    }
                }
                catch (Exception e)
                {
                    Logger.ErrorS("srv", $"Exception loading module '{path}':\n{e.ToStringBetter()}");
                    return false;
                }
            }
            Logger.DebugS("res.mod", $"DONE loading modules: {sw.Elapsed}");

            return true;
        }

        private static IEnumerable<ResourcePath> TopologicalSortModules(
            IEnumerable<KeyValuePair<string, (ResourcePath Path, string[] references)>> modules)
        {
            var elems = modules.ToDictionary(
                node => node.Key,
                node => (node.Value.Path, refs: new HashSet<string>(node.Value.references)));

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

        public void LoadGameAssembly(Stream assembly, Stream? symbols = null, bool skipVerify = false)
        {
            if (!skipVerify && !MakeTypeChecker().CheckAssembly(assembly))
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

        public void LoadGameAssembly(string diskPath, bool skipVerify = false)
        {
            if (!skipVerify && !MakeTypeChecker().CheckAssembly(diskPath))
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
                    LoadGameAssembly(path, skipVerify: false);
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
                using (gameDll)
                {
                    Logger.DebugS("srv", $"Loading {assemblyName} DLL");

                    // see if debug info is present
                    if (_res.TryContentFileRead(new ResourcePath($@"/Assemblies/{assemblyName}.pdb"),
                        out var gamePdb))
                    {
                        using (gamePdb)
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
                    if (!SandboxingEnabled)
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
            AssemblyLoadContext.Default.Resolving += DefaultOnResolving;
        }

        private Assembly? DefaultOnResolving(AssemblyLoadContext ctx, AssemblyName name)
        {
            // We have to hook AssemblyLoadContext.Default.Resolving so that C# interactive loads assemblies correctly.
            // Otherwise it would load the assemblies a second time which is an amazing way to have everything break.
            if (_useLoadContext)
            {
                _logManager.GetSawmill("res.mod").Debug($"RESOLVING DEFAULT: {name}");
                foreach (var module in LoadedModules)
                {
                    if (module.GetName().Name == name.Name)
                    {
                        return module;
                    }
                }

                foreach (var module in _sideModules)
                {
                    if (module.GetName().Name == name.Name)
                    {
                        return module;
                    }
                }
            }

            return null;
        }

        private AssemblyTypeChecker MakeTypeChecker()
        {
            return new(_res, Logger.GetSawmill("res.typecheck"))
            {
                VerifyIL = SandboxingEnabled,
                DisableTypeCheck = !SandboxingEnabled,
                ExtraRobustLoader = VerifierExtraLoadHandler
            };
        }
    }
}
