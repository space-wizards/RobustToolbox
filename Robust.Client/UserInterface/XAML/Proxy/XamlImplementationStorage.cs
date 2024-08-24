#if TOOLS
using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Log;
using Robust.Xaml;

namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// This is a utility class that tracks the relationship between resource file names,
/// Xamlx-compatible <see cref="Uri"/>s, <see cref="Type"/>s that are interested in a
/// given file, and implementations of Populate.
/// </summary>
internal sealed class XamlImplementationStorage
{
    /// <summary>
    /// For each filename, we store its last known <see cref="Uri"/>.
    /// </summary>
    /// <remarks>
    /// When we compile the new implementation, we will use the same <see cref="Uri"/>.
    /// </remarks>
    private readonly Dictionary<string, Uri> _fileUri = new();

    /// <summary>
    /// For each filename, we store its last known content.
    /// </summary>
    /// <remarks>
    /// This is known even for AOT-compiled code -- therefore, we can use this table
    /// to convert an AOT-compiled Control to a JIT-compiled one.
    /// </remarks>
    private readonly Dictionary<string, string> _fileContent = new();

    /// <summary>
    /// For each filename, we store the type interested in this file.
    /// </summary>
    private readonly Dictionary<string, Type> _fileType = new();

    /// <summary>
    /// For each type, store the JIT-compiled implementation of Populate.
    /// </summary>
    /// <remarks>
    /// If no such implementation exists, then methods that would normally
    /// find and call a JIT'ed implementation will do nothing and return
    /// false instead. As an ultimate result, the AOT'ed implementation
    /// will be used.
    /// </remarks>
    private readonly Dictionary<Type, MethodInfo> _populateImplementations = new();

    private readonly ISawmill _sawmill;
    private readonly XamlJitDelegate _jitDelegate;

    /// <summary>
    /// Create the storage.
    /// </summary>
    /// <remarks>
    /// It would be weird to call this from any type outside of
    /// <see cref="Robust.Client.UserInterface.XAML.Proxy" />.
    /// </remarks>
    /// <param name="sawmill">the (shared) logger</param>
    /// <param name="jitDelegate">
    ///     a delegate that calls the
    ///     <see cref="XamlJitCompiler"/>, possibly handling errors
    /// </param>
    public XamlImplementationStorage(ISawmill sawmill, XamlJitDelegate jitDelegate)
    {
        _sawmill = sawmill;
        _jitDelegate = jitDelegate;
    }

    /// <summary>
    /// Inspect <paramref name="assembly" /> for types that declare a <see cref="XamlMetadataAttribute"/>.
    /// </summary>
    /// <remarks>
    /// We can only do hot reloading if we know this basic information.
    ///
    /// Note that even release-mode content artifacts contain this attribute.
    /// </remarks>
    /// <param name="assembly">the assembly</param>
    /// <returns>an IEnumerable of types with xaml metadata</returns>
    private IEnumerable<(Type, XamlMetadataAttribute)> TypesWithXamlMetadata(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<XamlMetadataAttribute>() is not { } attr)
            {
                continue;
            }

            yield return (type, attr);
        }

    }

    /// <summary>
    /// Add all Xaml-annotated types from <paramref name="assembly" /> to this storage.
    /// </summary>
    /// <remarks>
    /// We don't JIT these types, but we store enough info that we could JIT
    /// them if we wanted to.
    /// </remarks>
    /// <param name="assembly">an assembly</param>
    public void Add(Assembly assembly)
    {
        foreach (var (type, metadata) in TypesWithXamlMetadata(assembly))
        {
            // this can fail, but if it does, that means something is _really_ wrong
            // with the compiler, or someone tried to write their own Xaml metadata
            Uri uri;
            try
            {
                uri = new Uri(metadata.Uri);
            }
            catch (UriFormatException)
            {
                throw new InvalidProgramException(
                    $"XamlImplementationStorage encountered an malformed Uri in the metadata for {type.FullName}: " +
                    $"{metadata.Uri}. this is a bug in XamlAotCompiler"
                );
            }

            var fileName = metadata.FileName;
            var content = metadata.Content;

            _fileUri[fileName] = uri;
            _fileContent[fileName] = content;

            if (!_fileType.TryAdd(fileName, type))
            {
                throw new InvalidProgramException(
                    $"XamlImplementationStorage observed that two types were interested in the same Xaml filename: " +
                    $"{fileName}. ({type.FullName} and {_fileType[fileName].FullName}). this is a bug in XamlAotCompiler"
                );
            }
        }
    }

    /// <summary>
    /// Quietly JIT every type with XAML metadata.
    /// </summary>
    /// <remarks>
    /// This should have no visible effect except that the <see cref="XamlJitDelegate"/>
    /// may dump some info messages into the terminal about cases where the
    /// hot reload failed.
    /// </remarks>
    public void ForceReloadAll()
    {
        foreach (var (fileName, fileContent) in _fileContent)
        {
            SetImplementation(fileName, fileContent, true);
        }
    }

    /// <summary>
    /// Return true if calling <see cref="SetImplementation" /> on <paramref name="fileName" /> would not be a no-op.
    /// </summary>
    /// <remarks>
    /// That is: if some type cares about the contents of <paramref name="fileName" />.
    /// </remarks>
    /// <param name="fileName">the filename</param>
    /// <returns>true if not a no-op</returns>
    public bool CanSetImplementation(string fileName)
    {
        return _fileType.ContainsKey(fileName);
    }

    /// <summary>
    /// Replace the implementation of <paramref name="fileName"/> by JIT-ing
    /// <paramref name="fileContent"/>.
    /// </summary>
    /// <remarks>
    /// If nothing cares about the implementation of <paramref name="fileName"/>, then this will do nothing.
    /// </remarks>
    /// <param name="fileName">the name of the file whose implementation should be replaced</param>
    /// <param name="fileContent">the new implementation</param>
    /// <param name="quiet">if true, then don't bother to log</param>
    public void SetImplementation(string fileName, string fileContent, bool quiet)
    {
        if (!_fileType.TryGetValue(fileName, out var type))
        {
            _sawmill.Warning($"SetImplementation called with {fileName}, but no types care about its contents");
            return;
        }

        var uri =
            _fileUri.GetValueOrDefault(fileName) ??
            throw new InvalidProgramException("file URI missing (this is a bug in ImplementationStorage)");

        if (!quiet)
        {
            _sawmill.Debug($"replacing {fileName} for {type}");
        }
        var impl = _jitDelegate(type, uri, fileName, fileContent);
        if (impl != null)
        {
            _populateImplementations[type] = impl;
        }
        _fileContent[fileName] = fileContent;
    }

    /// <summary>
    /// Call the JITed implementation of Populate on a XAML-associated object <paramref name="o"/>.
    ///
    /// If no JITed implementation exists, return false.
    /// </summary>
    /// <param name="t">the static type of <paramref name="o"/></param>
    /// <param name="o">an instance of <paramref name="t"/> (can be a subclass)</param>
    /// <returns>true if a JITed implementation existed</returns>
    public bool Populate(Type t, object o)
    {
        if (!_populateImplementations.TryGetValue(t, out var implementation))
        {
            // pop out if we never JITed anything
            return false;
        }

        implementation.Invoke(null, [null, o]);
        return true;
    }
}
#endif
