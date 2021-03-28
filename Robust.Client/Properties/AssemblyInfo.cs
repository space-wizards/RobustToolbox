using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Robust.UnitTesting")]
[assembly: InternalsVisibleTo("Robust.Lite")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

#if NET5_0
[module: SkipLocalsInit]
#endif
