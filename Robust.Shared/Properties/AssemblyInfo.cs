using System.Runtime.CompilerServices;

// The following allows another friend assembly access to the types marked as internal.
// SS14 engine assemblies are friends.
// This way internal is "Content can't touch this".
[assembly: InternalsVisibleTo("Robust.Server")]
[assembly: InternalsVisibleTo("Robust.Client")]
[assembly: InternalsVisibleTo("Robust.Lite")]
[assembly: InternalsVisibleTo("Robust.UnitTesting")]
[assembly: InternalsVisibleTo("OpenToolkit.GraphicsLibraryFramework")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Gives access to Castle(Moq)
[assembly: InternalsVisibleTo("Content.Benchmarks")]
[assembly: InternalsVisibleTo("Robust.Benchmarks")]

#if NET5_0
[module: SkipLocalsInit]
#endif
