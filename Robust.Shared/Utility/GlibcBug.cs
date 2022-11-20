using System;
using System.Runtime.InteropServices;
using C = System.Console;

namespace Robust.Shared.Utility;

internal static unsafe class GlibcBug
{
    /// <summary>
    /// Check for the glibc 2.35 DSO bug and log a warning if necessary.
    /// </summary>
    public static void Check()
    {
        if (!OperatingSystem.IsLinux())
            return;

        try
        {
            var versionString = Marshal.PtrToStringUTF8((IntPtr) gnu_get_libc_version());
            var version = Version.Parse(versionString!);
            var badVersion = new Version(2, 35);
            if (version >= badVersion)
            {
                C.ForegroundColor = ConsoleColor.Yellow;
                C.WriteLine($"!!!WARNING!!!: glibc {badVersion} or higher detected (you have {version}).");
                C.WriteLine("If anything misbehaves (weird native crashes, library load failures), try setting GLIBC_TUNABLES=glibc.rtld.dynamic_sort=1 as environment variable.");
                C.WriteLine("This is a severe glibc bug introduced in glibc 2.35. See https://github.com/space-wizards/RobustToolbox/issues/2563 for details");
                C.WriteLine("We cannot detect if you are susceptible or whether you have correctly applied the fix. This warning cannot be removed.");
                C.ResetColor();
            }
        }
        catch
        {
            // Couldn't figure out glibc version, whatever.
            // Hell maybe you're not even using glibc.
        }
    }

    [DllImport("libc.so.6")]
    private static extern byte* gnu_get_libc_version();
}
