namespace Robust.Shared.Interop.RobustNative;

internal static class RobustNativeDll
{
    private const string DllName = "robust-native";

    private static readonly Lock LoadLock = new();

    public static bool IsClientProcess;
    public static bool IsServerProcess;

    private static nint _robustNativeHandle;
    private static Exception? _loadFailed;

    static RobustNativeDll()
    {
        NativeLibrary.SetDllImportResolver(typeof(RobustNativeDll).Assembly,
            (name, _, _) =>
            {
                if (name == DllName)
                    return LoadRobustNative();

                return 0;
            });
    }

    public static nint LoadRobustNative()
    {
        using var _ = LoadLock.EnterScope();

        if (_robustNativeHandle != 0)
            return _robustNativeHandle;

        if (_loadFailed != null)
            throw _loadFailed;

        try
        {
            _robustNativeHandle = LoadCore();
            return _robustNativeHandle;
        }
        catch (Exception e)
        {
            _loadFailed = e;
            throw _loadFailed;
        }
    }

    private static nint LoadCore()
    {
        var dllName = GetDllName();
#if WINDOWS
        return NativeLibrary.Load(dllName, typeof(RobustNativeDll).Assembly, DllImportSearchPath.SafeDirectories);
#elif UNIX
#if MACOS
        dllName = $"lib{dllName}.dylib";
#else
        dllName = $"lib{dllName}.so";
#endif
        // On Unix platforms we *need* to load the native lib with RTLD_DEEPBIND | RTLD_LOCAL
        // To avoid issues with symbol collisions.
        var searchDirectories = (string?)AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES");

        foreach (var dir in searchDirectories?.Split(':') ?? Array.Empty<string>()) {
            var libraryPath = Path.Combine(dir, dllName);

            var attempt = Libc.dlopen(libraryPath, Libc.RTLD_LAZY | Libc.RTLD_DEEPBIND | Libc.RTLD_LOCAL);
            if (attempt != 0)
                return attempt;
        }

        throw new DllNotFoundException($"Unable to locatee {dllName}");
#endif
    }

    private static string GetDllName()
    {
#if DEVELOPMENT
        // Always load universal on dev builds to avoid issues with test processes.
        return "robust_native_universal";
#else
        if (IsClientProcess && IsServerProcess)
            return "robust_native_universal";

        if (IsClientProcess)
            return "robust_native_client";

        if (IsServerProcess)
            return "robust_native_server";

        throw new InvalidOperationException("Is neither server or client process???");
#endif
    }

}
