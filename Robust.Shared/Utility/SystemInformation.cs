using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Robust.Shared.Utility;

/// <summary>
/// Helper class to get additional system information.
/// </summary>
internal static class SystemInformation
{
    public static string GetProcessorModel()
    {
        if (X86Base.IsSupported)
        {
            var name = GetProcessorModelX86();
            if (name != null)
                return name;
        }

        if (OperatingSystem.IsMacOS())
        {
            var name = GetProcessorModelMacOS();
            if (name != null)
                return name;
        }

        // TODO: ask OS as fallback for when x86 CPUID isn't available on Windows and Linux.

        return "Unknown processor model";
    }

    private static string? GetProcessorModelX86()
    {
        unchecked
        {
            var (max, _, _, _) = X86Base.CpuId((int)0x8000000u, 0);
            if (max < (int)0x80000004u)
                return null;

            Span<int> name = stackalloc int[12];

            for (var i = 0; i < 3; i++)
            {
                var (eax, ebx, ecx, edx) = X86Base.CpuId((int)0x80000002u + i, 0);

                name[i * 4 + 0] = eax;
                name[i * 4 + 1] = ebx;
                name[i * 4 + 2] = ecx;
                name[i * 4 + 3] = edx;
            }

            var bytes = MemoryMarshal.Cast<int, byte>(name).TrimEnd((byte) 0);
            var model = Encoding.UTF8.GetString(bytes).TrimEnd();
            return model;
        }
    }

    private static unsafe string? GetProcessorModelMacOS()
    {
        fixed (byte* sysctlName = "machdep.cpu.brand_string"u8)
        {
            nint len = 0;
            var err = sysctlbyname(sysctlName, null, &len, null, 0);
            if (err != 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            Span<byte> brand = stackalloc byte[(int)len];
            fixed (byte* brandPtr = brand)
            {
                err = sysctlbyname(sysctlName, brandPtr, &len, null, 0);
            }

            if (err != 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            return Encoding.UTF8.GetString(brand).TrimEnd();
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static unsafe extern int sysctlbyname(
        byte* name,
        void* oldp,
        nint* oldlenp,
        void* newp,
        nint newlen);
}
