#if TOOLS
using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Log;

namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// This is a utility class that tracks the relationship between resource file names,
/// Xamlx-compatible Uris, Types that are interested in a given file, and implementations
/// of Populate.
///
/// The specific relationships are documented below.
/// </summary>
internal sealed class XamlImplementationStorage
{
    /// <summary>
    /// For each filename, we store its last known Uri.
    ///
    /// When we compile the new implementation, we will use the same Uri.
    /// </summary>
    Dictionary<string, Uri> _fileUri = new();

    /// <summary>
    /// For each filename, we store its last known content.
    ///
    /// This is known even for AOT-compiled code -- therefore, we can use this table
    /// to convert an AOT-compiled Control to a JIT-compiled one.
    /// </summary>
    Dictionary<string, string> _fileContent = new();

    /// <summary>
    /// For each filename, we store the list of types interested in this file.
    ///
    /// This may be more than one -- that is, multiple Controls can be associated
    /// with the same XAML file.
    ///
    /// At time of writing, for instance, StrippingMenu has no *.xaml file, and
    /// therefore it uses DefaultWindow.xaml.
    /// </summary>
    Dictionary<string, List<Type>> _fileTypes = new();

    /// <summary>
    /// For each type, store the JIT-compiled implementation of Populate.
    ///
    /// Of course, if no such implementation exists, then the AOT-compiled
    /// implementation will be used.
    /// </summary>
    Dictionary<Type, MethodInfo> _populateImplementations = new();

    ISawmill _sawmill;
    XamlJitDelegate _jitDelegate;

    /// <summary>
    /// Create the storage. It would be weird to call this from any type other than
    /// XamlHotReloadManager
    /// </summary>
    /// <param name="sawmill">the (shared) logger</param>
    /// <param name="jitDelegate">a delegate that calls the XamlJitCompiler, possibly handling errors</param>
    public XamlImplementationStorage(ISawmill sawmill, XamlJitDelegate jitDelegate)
    {
        _sawmill = sawmill;
        _jitDelegate = jitDelegate;
    }

    /// <summary>
    /// Inspect `a` for types that declare a XamlMetadataAttribute.
    ///
    /// We can only do hot reloading if we know this basic information.
    ///
    /// Note that even release-mode content artifacts contain this Attribute.
    /// </summary>
    /// <param name="a">the assembly</param>
    /// <returns>an Enumerable of types with Xaml metadata</returns>
    private IEnumerable<(Type, XamlMetadataAttribute)> TypesWithXamlMetadata(Assembly a)
    {
        foreach (var type in a.GetTypes())
        {
            if (type.GetCustomAttribute<XamlMetadataAttribute>() is not { } attr)
            {
                continue;
            }

            yield return (type, attr);
        }

    }

    /// <summary>
    /// Add all Xaml-affiliated types from Assembly `a` to this storage.
    ///
    /// For starters, we store what they're interested in and we store a copy of their
    /// Xaml code. (We don't JIT anything.)
    ///
    /// But we do store enough info to JIT stuff if we want to!
    /// </summary>
    /// <param name="a">an assembly</param>
    public void Add(Assembly a)
    {
        foreach (var (type, metadata) in TypesWithXamlMetadata(a))
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

            List<Type> types;
            _fileTypes[fileName] = (types = _fileTypes.GetValueOrDefault(fileName) ?? []);
            types.Add(type);
        }
    }

    /// <summary>
    /// Quietly JIT every type with XAML metadata.
    ///
    /// This should have no visible effect except that the XamlJitDelegate may dump some
    /// warnings into the terminal about cases where the hot reload failed.
    /// </summary>
    public void ForceReloadAll()
    {
        foreach (var (fileName, fileContent) in _fileContent)
        {
            SetImplementation(fileName, fileContent, true);
        }
    }

    /// <summary>
    /// Return true if `SetImplementation(fileName)` would not be a no-op.
    ///
    /// (That is, if some type cares about the contents of `fileName`.)
    /// </summary>
    /// <param name="fileName">the filename</param>
    /// <returns>true if not a no-op</returns>
    public bool CanSetImplementation(string fileName)
    {
        return _fileTypes.ContainsKey(fileName);
    }

    /// <summary>
    /// Replace the implementation of `fileName` by JIT-ing `fileContent`.
    ///
    /// If nothing cares about the implementation of `fileName`, then this will do nothing.
    /// </summary>
    /// <param name="fileName">the name of the file whose implementation should be replaced</param>
    /// <param name="fileContent">the new implementation</param>
    /// <param name="quiet">if true, then don't bother to log</param>
    public void SetImplementation(string fileName, string fileContent, bool quiet)
    {
        if (!_fileTypes.TryGetValue(fileName, out var types))
        {
            _sawmill.Warning($"SetImplementation called with {fileName}, but no types care about its contents");
            return;
        }

        var uri =
            _fileUri.GetValueOrDefault(fileName) ??
            throw new InvalidProgramException("file URI missing (this is a bug in ImplementationStorage)");

        foreach (var type in types)
        {
            if (!quiet)
            {
                _sawmill.Debug($"replacing {fileName} for {type}");
            }
            var impl = _jitDelegate(type, uri, fileName, fileContent);
            if (impl != null)
            {
                _populateImplementations[type] = impl;
            }
        }
        _fileContent[fileName] = fileContent;
    }

    /// <summary>
    /// Call the JITed implementation of Populate on a XAML-associated object `o`.
    ///
    /// If no JITed implementation exists, return false.
    /// </summary>
    /// <param name="t">the static type of `o`</param>
    /// <param name="o">an instance of `t` (can be a subclass)</param>
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
