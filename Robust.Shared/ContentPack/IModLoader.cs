using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     The mod loader is in charge of loading content assemblies and managing them.
    /// </summary>
    public interface IModLoader
    {
        /// <summary>
        ///     All directly loaded content assemblies.
        /// </summary>
        IEnumerable<Assembly> LoadedModules { get; }

        Assembly GetAssembly(string name);

        /// <summary>
        ///     Adds a testing callbacks that will be passed to <see cref="GameShared.SetTestingCallbacks"/>.
        /// </summary>
        void SetModuleBaseCallbacks(ModuleTestingCallbacks testingCallbacks);

        bool IsContentAssembly(Assembly typeAssembly);
    }

    internal interface IModLoaderInternal : IModLoader
    {
        /// <summary>
        ///     Loads all content assemblies from the specified resource directory and filename prefix.
        /// </summary>
        /// <param name="mountPath">The directory in which to look for assemblies.</param>
        /// <param name="filterPrefix">The prefix files need to have to be considered. e.g. <c>Content.</c></param>
        /// <returns>True if all modules loaded successfully. False if there were load errors.</returns>
        bool TryLoadModulesFrom(ResourcePath mountPath, string filterPrefix);

        /// <summary>
        ///     Loads an assembly into the current AppDomain.
        /// </summary>
        /// <param name="assembly">Byte array of the assembly.</param>
        /// <param name="symbols">Optional byte array of the debug symbols.</param>
        /// <param name="skipVerify">Whether to skip checking the loaded assembly for sandboxing.</param>
        void LoadGameAssembly(Stream assembly, Stream? symbols = null, bool skipVerify = false);

        /// <summary>
        ///     Loads an assembly into the current AppDomain.
        /// </summary>
        void LoadGameAssembly(string diskPath, bool skipVerify = false);

        /// <summary>
        ///     Broadcasts a run level change to all loaded entry point.
        /// </summary>
        /// <param name="level">New level</param>
        void BroadcastRunLevel(ModRunLevel level);

        void BroadcastUpdate(ModUpdateLevel level, FrameEventArgs frameEventArgs);

        /// <summary>
        ///     Tries to load an assembly from a resource manager into the current appdomain.
        /// </summary>
        /// <param name="assemblyName">File name of the assembly inside of the ./Assemblies folder in the resource manager.</param>
        /// <returns>If the assembly was successfully located and loaded.</returns>
        bool TryLoadAssembly(string assemblyName);

        void SetUseLoadContext(bool useLoadContext);
        void SetEnableSandboxing(bool sandboxing);

        Func<string, Stream?>? VerifierExtraLoadHandler { get; set; }
    }
}
