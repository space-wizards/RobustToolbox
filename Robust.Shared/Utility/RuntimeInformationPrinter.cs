using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Robust.Shared.Utility;

internal static class RuntimeInformationPrinter
{
    public static string[] GetInformationDump()
    {
        var version = typeof(RuntimeInformationPrinter).Assembly.GetName().Version;

        return new[]
        {
            $"OS: {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}",
            $".NET Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.RuntimeIdentifier}",
            $"Server GC: {GCSettings.IsServerGC}",
            $"Processor: {Environment.ProcessorCount}x {SystemInformation.GetProcessorModel()}",
            $"Architecture: {RuntimeInformation.ProcessArchitecture}",
            $"Robust Version: {version}",
            $"Compile Options: {string.Join(';', GetCompileOptions())}",
            $"Intrinsics: {string.Join(';', SystemInformation.GetIntrinsics())}",
        };
    }

    private static List<string> GetCompileOptions()
    {
        var options = new List<string>();

#if DEVELOPMENT
        options.Add("DEVELOPMENT");
#endif

#if FULL_RELEASE
        options.Add("FULL_RELEASE");
#endif

#if TOOLS
        options.Add("TOOLS");
#endif

#if DEBUG
        options.Add("DEBUG");
#endif

#if RELEASE
        options.Add("RELEASE");
#endif

#if EXCEPTION_TOLERANCE
        options.Add("EXCEPTION_TOLERANCE");
#endif

#if CLIENT_SCRIPTING
        options.Add("CLIENT_SCRIPTING");
#endif

#if USE_SYSTEM_SQLITE
        options.Add("USE_SYSTEM_SQLITE");
#endif

        return options;
    }
}
