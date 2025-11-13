using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
[module: SkipLocalsInit]
#endif

[assembly: InternalsVisibleTo("Robust.Shared")]
[assembly: InternalsVisibleTo("Robust.Server")]
[assembly: InternalsVisibleTo("Robust.Client")]
[assembly: InternalsVisibleTo("Robust.UnitTesting")]

#if DEVELOPMENT
[assembly: InternalsVisibleTo("Robust.Benchmarks")]
[assembly: InternalsVisibleTo("Content.Benchmarks")]
#endif
