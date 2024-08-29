using System;

namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// This service provides a proxy for Populate, which is the generated function that
/// initializes the UI objects of a Xaml widget.
/// </summary>
/// <remarks>
/// The proxy can always return false: in that case, a Xaml widget will self-populate
/// as usual. This is the behavior on Release builds.
///
/// However, it can also call into an externally-provided implementation of the Xaml
/// widget.
///
/// No source of externally-provided implementations actually exists, by default --
/// you will need to call SetImplementation with a blob of xaml source code to provide
/// one. <see cref="IXamlHotReloadManager" /> is an example of a service that calls into
/// that functionality.
/// </remarks>
internal interface IXamlProxyManager
{
    /// <summary>
    /// Initialize creates the <see cref="IXamlProxyManager"/>.
    /// </summary>
    /// <remarks>
    /// If the <see cref="IXamlProxyManager" /> is not a stub, then it will spy on the
    /// assembly list (from <see cref="Robust.Shared.Reflection.IReflectionManager" />)
    /// and find <see cref="XamlMetadataAttribute" /> entries on the loaded types.
    /// </remarks>
    void Initialize();

    /// <summary>
    /// Return true if at least one <see cref="Type"/> in the current project expects its XAML
    /// to come from a file with the given name.
    /// </summary>
    /// <remarks>
    /// This method supports code that is trying to figure out what name the build process
    /// would have assigned to a resource file. A caller can try a few candidate names and use
    /// its "yes" to continue.
    ///
    /// This method is very fast, so it's OK to hammer it!
    ///
    /// Also, on a non-tools build, this always returns false.
    /// </remarks>
    /// <param name="fileName">the filename</param>
    /// <returns>true if expected</returns>
    bool CanSetImplementation(string fileName);

    /// <summary>
    /// Replace the implementation of <paramref name="fileName"/> with <paramref name="fileContent" />,
    /// compiling it if needed.
    ///
    /// All types based on <paramref name="fileName" /> will be recompiled.
    /// </summary>
    /// <remarks>
    /// This may fail and the caller won't be notified. (There will usually be logs.)
    ///
    /// On a non-tools build, this fails silently.
    /// </remarks>
    /// <param name="fileName">the name of the file</param>
    /// <param name="fileContent">the new content of the file</param>
    void SetImplementation(string fileName, string fileContent);

    /// <summary>
    /// If we have a JIT version of the XAML code for <paramref name="t" />, then call
    /// the new implementation on <paramref name="o" />.
    /// </summary>
    /// <remarks>
    /// <paramref name="o" /> may be a subclass of <paramref name="t" />.
    /// </remarks>
    /// <param name="t">the static type of the object</param>
    /// <param name="o">the object</param>
    /// <returns>true if we called a hot reloaded implementation</returns>
    bool Populate(Type t, object o);
}
