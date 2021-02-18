using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Robust.UnitTesting")]
[assembly: InternalsVisibleTo("Robust.Lite")]

#if NET5_0
[module: SkipLocalsInit]
#endif
