using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;
using ArmAes = System.Runtime.Intrinsics.Arm.Aes;

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

    public static List<string> GetIntrinsics()
    {
        var options = new List<string>();

        // - No put the oof back, hello?

        // x86

        if (X86Aes.IsSupported)
            options.Add(nameof(X86Aes));

        if (Avx.IsSupported)
            options.Add(nameof(Avx));

        if (Avx2.IsSupported)
            options.Add(nameof(Avx2));

        if (Avx512BW.IsSupported)
            options.Add(nameof(Avx512BW));

        if (Avx512BW.VL.IsSupported)
            options.Add(nameof(Avx512BW) + ".VL");

        if (Avx512CD.IsSupported)
            options.Add(nameof(Avx512CD));

        if (Avx512CD.VL.IsSupported)
            options.Add(nameof(Avx512CD) + ".VL");

        if (Avx512DQ.IsSupported)
            options.Add(nameof(Avx512DQ));

        if (Avx512DQ.VL.IsSupported)
            options.Add(nameof(Avx512DQ) + ".VL");

        if (Avx512F.IsSupported)
            options.Add(nameof(Avx512F));

        if (Avx512F.VL.IsSupported)
            options.Add(nameof(Avx512F) + ".VL");

        if (Avx512Vbmi.IsSupported)
            options.Add(nameof(Avx512Vbmi));

        if (Avx512Vbmi.VL.IsSupported)
            options.Add(nameof(Avx512Vbmi) + ".VL");

        if (Bmi1.IsSupported)
            options.Add(nameof(Bmi1));

        if (Bmi2.IsSupported)
            options.Add(nameof(Bmi2));

        if (Fma.IsSupported)
            options.Add(nameof(Fma));

        if (Lzcnt.IsSupported)
            options.Add(nameof(Lzcnt));

        if (Pclmulqdq.IsSupported)
            options.Add(nameof(Pclmulqdq));

        if (Popcnt.IsSupported)
            options.Add(nameof(Popcnt));

        if (Sse.IsSupported)
            options.Add(nameof(Sse));

        if (Sse2.IsSupported)
            options.Add(nameof(Sse2));

        if (Sse3.IsSupported)
            options.Add(nameof(Sse3));

        if (Ssse3.IsSupported)
            options.Add(nameof(Ssse3));

        if (Sse41.IsSupported)
            options.Add(nameof(Sse41));

        if (Sse42.IsSupported)
            options.Add(nameof(Sse42));

        if (X86Base.IsSupported)
            options.Add(nameof(X86Base));

        // ARM

        if (AdvSimd.IsSupported)
            options.Add(nameof(AdvSimd));

        if (ArmAes.IsSupported)
            options.Add(nameof(ArmAes));

        if (ArmBase.IsSupported)
            options.Add(nameof(ArmBase));

        if (Crc32.IsSupported)
            options.Add(nameof(Crc32));

        if (Dp.IsSupported)
            options.Add(nameof(Dp));

        if (Rdm.IsSupported)
            options.Add(nameof(Rdm));

        if (Sha1.IsSupported)
            options.Add(nameof(Sha1));

        if (Sha256.IsSupported)
            options.Add(nameof(Sha256));

        // WASM

        if (PackedSimd.IsSupported)
            options.Add(nameof(PackedSimd));

        return options;
    }
}
