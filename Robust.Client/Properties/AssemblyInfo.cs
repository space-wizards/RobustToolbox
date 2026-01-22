using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Robust.UnitTesting")]
[assembly: InternalsVisibleTo("Robust.Client.WebView")]
[assembly: InternalsVisibleTo("Robust.Lite")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Robust.Benchmarks")]
[assembly: InternalsVisibleTo("Robust.Client.Tests")]
[assembly: InternalsVisibleTo("Robust.Client.IntegrationTests")]

#if NET5_0_OR_GREATER
[module: SkipLocalsInit]
#endif
