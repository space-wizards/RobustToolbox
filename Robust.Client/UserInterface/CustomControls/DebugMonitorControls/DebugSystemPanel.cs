using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;
using ArmAes = System.Runtime.Intrinsics.Arm.Aes;

namespace Robust.Client.UserInterface.CustomControls.DebugMonitorControls;

internal sealed class DebugSystemPanel : PanelContainer
{
    public DebugSystemPanel()
    {
        var contents = new Label
        {
            FontColorShadowOverride = Color.Black,
        };
        AddChild(contents);

        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = new Color(35, 134, 37, 138),
            ContentMarginLeftOverride = 5,
            ContentMarginRightOverride = 5,
            ContentMarginTopOverride = 5,
            ContentMarginBottomOverride = 5,
        };

        MouseFilter = contents.MouseFilter = MouseFilterMode.Ignore;

        HorizontalAlignment = HAlignment.Left;

        var version = typeof(DebugSystemPanel).Assembly.GetName().Version;

        contents.Text = @$"OS: {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}
.NET Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.RuntimeIdentifier}
Server GC: {GCSettings.IsServerGC}
Processor: {Environment.ProcessorCount}x {SystemInformation.GetProcessorModel()}
Architecture: {RuntimeInformation.ProcessArchitecture}
Robust Version: {version}
Compile Options: {GetCompileOptions()}
Intrinsics: {GetIntrinsics()}";
    }

    private static string GetCompileOptions()
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

        return string.Join(';', options);
    }

    private static string GetIntrinsics()
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

        return string.Join(';', options);
    }
}
