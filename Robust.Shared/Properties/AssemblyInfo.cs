using System.Runtime.CompilerServices;

// The following allows another friend assembly access to the types marked as internal.
// SS14 engine assemblies are friends.
// This way internal is "Content can't touch this".
[assembly: InternalsVisibleTo("Robust.Server")]
[assembly: InternalsVisibleTo("Robust.Client")]
[assembly: InternalsVisibleTo("Robust.UnitTesting")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Gives access to Castle(Moq)
