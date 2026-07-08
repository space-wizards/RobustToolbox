using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Robust.UnitTesting")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Robust.Benchmarks")]
[assembly: InternalsVisibleTo("Robust.Shared.IntegrationTests")]
[assembly: InternalsVisibleTo("Robust.Server.IntegrationTests")]
[assembly: InternalsVisibleTo("Robust.Server.Testing")]

#if NET5_0_OR_GREATER
[module: SkipLocalsInit]
#endif
